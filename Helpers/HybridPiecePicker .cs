using System;
using System.Collections.Generic;
using System.Linq;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.PiecePicking;

public class HybridPiecePicker : IPiecePicker
{
    private readonly IPiecePicker standard;
    private readonly IPiecePicker streaming;
    private readonly TorrentManager manager;

    public HybridPiecePicker(TorrentManager manager)
    {
        this.manager = manager;
        standard = new StandardPicker();
        streaming = new PriorityPicker(new StandardPicker());
    }

    public void Initialise(IPieceRequesterData torrentData)
    {
        standard.Initialise(torrentData);
        streaming.Initialise(torrentData);
    }

    public int PickPiece(IRequester peer, ReadOnlyBitField available,
        ReadOnlySpan<ReadOnlyBitField> otherPeers, int count,
        int startIndex, Span<PieceSegment> requests)
    {
        var file = manager.Files.FirstOrDefault(f => startIndex >= f.StartPieceIndex && startIndex <= f.EndPieceIndex);
        if (file != null && file.Priority == Priority.High)
            return streaming.PickPiece(peer, available, otherPeers, count, startIndex, requests);

        return standard.PickPiece(peer, available, otherPeers, count, startIndex, requests);
    }

    public bool IsInteresting(IRequester peer, ReadOnlyBitField available)
        => standard.IsInteresting(peer, available) || streaming.IsInteresting(peer, available);

    public int CancelRequests(IRequester peer, int startIndex, int endIndex, Span<PieceSegment> requests)
    {
        int cancelled = 0;
        cancelled += standard.CancelRequests(peer, startIndex, endIndex, requests);
        cancelled += streaming.CancelRequests(peer, startIndex, endIndex, requests);
        return cancelled;
    }

    public bool ContinueAnyExistingRequest(IRequester peer, ReadOnlyBitField available,
        int startIndex, int endIndex, int count, out PieceSegment segment)
    {
        return streaming.ContinueAnyExistingRequest(peer, available, startIndex, endIndex, count, out segment) ||
               standard.ContinueAnyExistingRequest(peer, available, startIndex, endIndex, count, out segment);
    }

    public bool ContinueExistingRequest(IRequester peer, int pieceIndex, int startOffset, out PieceSegment segment)
    {
        return streaming.ContinueExistingRequest(peer, pieceIndex, startOffset, out segment) ||
               standard.ContinueExistingRequest(peer, pieceIndex, startOffset, out segment);
    }

    public int CurrentReceivedCount() => standard.CurrentReceivedCount() + streaming.CurrentReceivedCount();

    public int CurrentRequestCount() => standard.CurrentRequestCount() + streaming.CurrentRequestCount();

    public IList<ActivePieceRequest> ExportActiveRequests()
    {
        return streaming.ExportActiveRequests()
            .Concat(standard.ExportActiveRequests())
            .ToList();
    }

    public void RequestRejected(IRequester peer, PieceSegment segment)
    {
        standard.RequestRejected(peer, segment);
        streaming.RequestRejected(peer, segment);
    }

    public bool ValidatePiece(IRequester peer, PieceSegment segment, out bool pieceComplete, HashSet<IRequester> peersInvolved)
    {
        return standard.ValidatePiece(peer, segment, out pieceComplete, peersInvolved) ||
               streaming.ValidatePiece(peer, segment, out pieceComplete, peersInvolved);
    }
}
