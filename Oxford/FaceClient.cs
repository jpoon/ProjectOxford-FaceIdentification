using Microsoft.ProjectOxford.Face;
using System.Threading.Tasks;
using Windows.Storage;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;

namespace Oxford
{
    public class FaceClient
    {
        private const string SubscriptionKey = "";
        private readonly string _groupName = "HungryHippo".ToLower();
        private readonly FaceServiceClient _faceClient;

        public Log Log
        {
            get; set;
        }

        public FaceClient()
        {
            _faceClient = new FaceServiceClient(SubscriptionKey);
        }

        public async Task UpsertPersonAsync(StorageFile image, string personName)
        {
            Log?.WriteLine($"Upsert person: {personName}");

            // person group
            try
            {
                var personGroup = await _faceClient.GetPersonGroupAsync(_groupName);
                Log?.WriteLine($"Found person group: {personGroup.Name}");
            }
            catch (FaceAPIException ex)
            {
                if (ex.ErrorCode == "PersonGroupNotFound")
                {
                    Log?.WriteLine("Creating person group");
                    await _faceClient.CreatePersonGroupAsync(_groupName, _groupName);
                }
            }

            // person
            var people = await _faceClient.GetPersonsAsync(_groupName);

            var personId = people.FirstOrDefault(p => p.Name.ToLowerInvariant() == personName.ToLowerInvariant())?.PersonId;
            if (personId == null || personId == Guid.Empty)
            {
                personId = (await _faceClient.CreatePersonAsync(_groupName, personName)).PersonId;
            }

            // face
            await Task.Run(async () =>
            {
                using (var fileStream = File.OpenRead(image.Path))
                {
                    await _faceClient.AddPersonFaceAsync(_groupName, (Guid)personId, fileStream, image.Path);
                    await _faceClient.TrainPersonGroupAsync(_groupName);

                    while (true)
                    {
                        var trainingStatus = await _faceClient.GetPersonGroupTrainingStatusAsync(_groupName);
                        Log?.WriteLine($"Training Status: {trainingStatus.Status.ToString()}");
                        if (trainingStatus.Status != Microsoft.ProjectOxford.Face.Contract.Status.Running)
                        {
                            break;
                        }

                        await Task.Delay(1500);
                    }
               }
            });
        }

        public async Task<List<IdentifiedPerson>> IdentifyAsync(StorageFile image)
        {
            var people = new List<IdentifiedPerson>();

            await Task.Run(async () =>
            {
                using (var fileStream = File.OpenRead(image.Path))
                {
                    // detect faces
                    var faces = await _faceClient.DetectAsync(fileStream);

                    // max 10 faces
                    faces = faces.Take(10).ToArray();
                    Log?.WriteLine($"Found {faces.Count()} number of faces.");

                    // identify each face
                    var identifyResult = await _faceClient.IdentifyAsync(_groupName, faces.Select(ff => ff.FaceId).ToArray());
                    foreach (var face in faces)
                    {
                        var identifiedPerson = new IdentifiedPerson { Face = face };

                        var identity = identifyResult.SingleOrDefault(i => i.FaceId == face.FaceId);

                        if (identity != null && identity.Candidates.Length > 0)
                        {
                            var candidate = identity.Candidates.OrderByDescending(c => c.Confidence).First();
                            var person = await _faceClient.GetPersonAsync(_groupName, candidate.PersonId);

                            Log?.WriteLine($"Found {person.Name}.");

                            identifiedPerson.PersonName = person.Name;
                            identifiedPerson.Face = faces.Single(f => f.FaceId == identity.FaceId);
                        }

                        people.Add(identifiedPerson);
                    }
                }
            });

            return people;
        }
    }
}
