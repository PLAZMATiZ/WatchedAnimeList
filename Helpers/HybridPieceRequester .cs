using System;
using System.Linq;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.PiecePicking;

public class HybridPieceRequester : IPieceRequester
{
    private readonly TorrentManager _manager;
    private IPieceRequesterData _data = null!;
    private IMessageEnqueuer _enqueuer = null!;
    private IPiecePicker _picker;

    private const int MaxRequestsPerFile = 12; // макс запитів на 1 файл
    private const double Alpha = 0.3; // для плавного підрахунку швидкості

    public HybridPieceRequester(TorrentManager manager)
    {
        _manager = manager;
        _picker = new StandardPicker();
    }

    public void Initialise(IPieceRequesterData data, IMessageEnqueuer enqueuer, ReadOnlySpan<ReadOnlyBitField> ignoringBitfields)
    {
        _data = data;
        _enqueuer = enqueuer;
        _picker.Initialise(data);
    }

    public void AddRequests(ReadOnlySpan<(IRequester Peer, ReadOnlyBitField Available)> peers)
    {
        var otherBitfields = new ReadOnlyBitField[peers.Length];
        for (int i = 0; i < peers.Length; i++)
            otherBitfields[i] = peers[i].Available;

        for (int i = 0; i < peers.Length; i++)
            AddRequests(peers[i].Peer, peers[i].Available, otherBitfields);
    }
    public void AddRequests(IRequester peer, ReadOnlyBitField available, ReadOnlySpan<ReadOnlyBitField> otherPeers)
    {
        if (_data == null || _enqueuer == null) return;

        var highFiles = _manager.Files
            .Where(f => f.Priority == Priority.High && !f.BitField.AllTrue)
            .Take(3)
            .ToArray();

        if (highFiles.Length == 0) return;

        int maxRequests = peer.MaxPendingRequests;

        for (int i = 0; i < highFiles.Length; i++)
        {
            int requestsForFile = i switch
            {
                0 => (int)(maxRequests * 0.5),
                1 => (int)(maxRequests * 0.25),
                2 => (int)(maxRequests * 0.25),
                _ => 0
            };
            if (requestsForFile <= 0) continue;

            var buffer = new PieceSegment[requestsForFile];
            int picked = _picker.PickPiece(peer, available, otherPeers, requestsForFile, highFiles[i].StartPieceIndex, buffer);
            if (picked > 0)
                _enqueuer.EnqueueRequests(peer, buffer.AsSpan(0, picked));
        }
    }


    public bool IsInteresting(IRequester peer, ReadOnlyBitField available)
        => _picker.IsInteresting(peer, available);

    public int PickPiece(IRequester peer, ReadOnlyBitField available, ReadOnlySpan<ReadOnlyBitField> otherPeers, int count, int startIndex, Span<PieceSegment> requests)
        => _picker.PickPiece(peer, available, otherPeers, count, startIndex, requests);

    public int CancelRequests(IRequester peer, int startIndex, int endIndex, Span<PieceSegment> requests)
        => _picker.CancelRequests(peer, startIndex, endIndex, requests);

    public bool ContinueAnyExistingRequest(IRequester peer, ReadOnlyBitField available, int startIndex, int endIndex, int count, out PieceSegment segment)
        => _picker.ContinueAnyExistingRequest(peer, available, startIndex, endIndex, count, out segment);

    public bool ContinueExistingRequest(IRequester peer, int pieceIndex, int startOffset, out PieceSegment segment)
        => _picker.ContinueExistingRequest(peer, pieceIndex, startOffset, out segment);

    public int CurrentReceivedCount() => _picker.CurrentReceivedCount();
    public int CurrentRequestCount() => _picker.CurrentRequestCount();

    public IList<ActivePieceRequest> ExportActiveRequests() => _picker.ExportActiveRequests();

    public void RequestRejected(IRequester peer, PieceSegment segment) => _picker.RequestRejected(peer, segment);
    public bool ValidatePiece(IRequester peer, PieceSegment segment, out bool pieceComplete, HashSet<IRequester> peersInvolved)
        => _picker.ValidatePiece(peer, segment, out pieceComplete, peersInvolved);

    public void CancelRequests(IRequester peer, int startIndex, int endIndex) { }
    public bool InEndgameMode => false;
}
