﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WatchedAnimeList.Helpers
{
    public class GoogleDriveHelper
    {
        private DriveService service;
        private const string AppName = "WachedAnimeList";
        private const string FolderName = "MyJsonFolder"; // назва папки на Google Drive

        public async Task InitAsync()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderPath = Path.Combine(documentsPath, "RE ZERO", "WachedAnimeList");

            UserCredential credential;

            using (var stream = GetEmbeddedCredentialsStream())
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { DriveService.Scope.DriveFile },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(Path.Combine(folderPath, "DriveToken"), true));
            }

            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName
            });
        }

        private Stream GetEmbeddedCredentialsStream()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("credentials.json"));

            if (resourceName == null)
                throw new Exception("Вбудований ресурс credentials.json не знайдено");

            return assembly.GetManifestResourceStream(resourceName);
        }

        private async Task<string> CreateOrGetFolderIdAsync(string folderName)
        {
            var listRequest = service.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";
            listRequest.Fields = "files(id, name)";

            var result = await listRequest.ExecuteAsync();

            var folder = result.Files.FirstOrDefault();
            if (folder != null)
                return folder.Id;

            // Якщо не знайдено — створюємо
            var metadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var createRequest = service.Files.Create(metadata);
            createRequest.Fields = "id";
            var createdFolder = await createRequest.ExecuteAsync();
            return createdFolder.Id;
        }


        public async Task UploadJsonAsync(string jsonText, string fileName)
        {
            string folderId = await CreateOrGetFolderIdAsync("Wat  chedAnimeList");

            // перевіряємо, чи такий файл вже є
            var listRequest = service.Files.List();
            listRequest.Q = $"name='{fileName}' and '{folderId}' in parents and trashed=false";
            listRequest.Fields = "files(id, name)";
            var files = await listRequest.ExecuteAsync();

            var existingFile = files.Files.FirstOrDefault();
            var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonText));

            if (existingFile != null)
            {
                var updateRequest = service.Files.Update(null, existingFile.Id, contentStream, "application/json");
                await updateRequest.UploadAsync();
            }
            else
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new[] { folderId }
                };

                var createRequest = service.Files.Create(fileMetadata, contentStream, "application/json");
                createRequest.Fields = "id";
                await createRequest.UploadAsync();
            }
        }
        public async Task<string> DownloadJsonAsync(string fileNameOnDrive)
        {
            try
            {
                string folderId = await CreateOrGetFolderIdAsync("WachedAnimeList");

                var listRequest = service.Files.List();
                listRequest.Q = $"name='{fileNameOnDrive}' and '{folderId}' in parents and trashed=false";
                listRequest.Fields = "files(id, name)";
                var files = await listRequest.ExecuteAsync();

                var file = files.Files.FirstOrDefault();
                if (file == null)
                    throw new Exception("Файл не знайдено в папці WachedAnimeList");

                var request = service.Files.Get(file.Id);
                using (var stream = new MemoryStream())
                {
                    await request.DownloadAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(stream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
