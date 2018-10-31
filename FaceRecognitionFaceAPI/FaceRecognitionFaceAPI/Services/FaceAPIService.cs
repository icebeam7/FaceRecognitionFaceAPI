using FaceRecognitionFaceAPI.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FaceRecognitionFaceAPI.Services
{
    public static class FaceAPIService
    {
        const string location = "tu-ubicacion-endpoint";
        public const string group = "webinar2";
        const string subscriptionKey = "tu-llave-suscripcion";
        static string DetectUriBase = $"https://{location}.api.cognitive.microsoft.com/face/v1.0/detect";
        static string IdentifyUriBase = $"https://{location}.api.cognitive.microsoft.com/face/v1.0/identify";
        static string PersonGroupUriBase = $"https://{location}.api.cognitive.microsoft.com/face/v1.0/persongroups/{group}";
        static string PersonUriBase = $"https://{location}.api.cognitive.microsoft.com/face/v1.0/persongroups/{group}/persons";
        static string GroupTrainUriBase = $"https://{location}.api.cognitive.microsoft.com/face/v1.0/persongroups/{group}/train";

        public static async Task<bool> CreateGroup(string name)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                    var uri = PersonGroupUriBase;
                    string json = "{\"name\":\"" + name + "\", \"userData\":\"Personas del grupo.\"}";
                    HttpContent content = new StringContent(json);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await client.PutAsync(uri, content);

                    return (response.StatusCode == System.Net.HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async static Task<string> CreatePerson(string name)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                    var uri = PersonUriBase;
                    string json = "{\"name\":\"" + name + "\", \"userData\":\"Miembro del grupo.\"}";
                    HttpContent content = new StringContent(json);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await client.PostAsync(uri, content);
                    string contentString = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(contentString);
                    string personId = obj["personId"].ToString();
                    return personId;
                }
            }
            catch (Exception ex)
            {
                return "Error";
            }
        }

        public async static Task<FaceModel> DetectFaces(MemoryStream stream)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                    string requestParameters = "returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";
                    string uri = $"{DetectUriBase}?{requestParameters}";
                    HttpResponseMessage response;

                    using (ByteArrayContent content = new ByteArrayContent(stream.ToArray()))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        response = await client.PostAsync(uri, content);
                        string contentString = await response.Content.ReadAsStringAsync();
                        var models = JsonConvert.DeserializeObject<List<FaceModel>>(contentString);
                        return models[0];
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async static Task<string> RegisterPerson(string id, MemoryStream stream)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                    string requestParameters = $"/{id}/persistedFaces";
                    string uri = $"{PersonUriBase}{requestParameters}";
                    HttpResponseMessage response;

                    using (ByteArrayContent content = new ByteArrayContent(stream.ToArray()))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        response = await client.PostAsync(uri, content);
                        string contentString = await response.Content.ReadAsStringAsync();
                        var obj = JObject.Parse(contentString);
                        string faceId = obj["persistedFaceId"].ToString();
                        return faceId;
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error al registrar persona";
            }
        }

        public async static Task<bool> TrainGroup()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                    string uri = GroupTrainUriBase;
                    HttpContent content = new StringContent("");
                    HttpResponseMessage response = await client.PostAsync(uri, content);

                    return (response.StatusCode == System.Net.HttpStatusCode.Accepted);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public async static Task<string> IdentifyPerson(MemoryStream stream)
        {
            try
            {
                var model = await DetectFaces(stream);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                    var uri = IdentifyUriBase;
                    string json = "{\"faceIds\":[\"" + model.FaceID + "\"], \"personGroupId\":\"" + group + "\", \"maxNumOfCandidatesReturned\":1, \"confidenceThreshold\":0.5}";
                    HttpContent content = new StringContent(json);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await client.PostAsync(uri, content);
                    string contentString = await response.Content.ReadAsStringAsync();
                    var array = JArray.Parse(contentString);
                    var obj = JObject.Parse(array[0].ToString());

                    string candidates = obj["candidates"].ToString();
                    var arrayCandidates = JArray.Parse(candidates);
                    var mejor = JObject.Parse(arrayCandidates[0].ToString());
                    string personId = mejor["personId"].ToString();
                    double confidence = double.Parse(mejor["confidence"].ToString());

                    string name = await GetPerson(personId);
                    return $"{name} ({confidence * 100}%)" ;
                }
            }
            catch (Exception ex)
            {
                return "Desconocido";
            }

        }

        public async static Task<string> GetPerson(string id)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                    string requestParameters = $"/{id}";
                    var uri = $"{PersonUriBase}{requestParameters}";
                    HttpResponseMessage response = await client.GetAsync(uri);
                    string contentString = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(contentString);
                    string name = obj["name"].ToString();
                    return name;
                }
            }
            catch (Exception ex)
            {
                return "Desconocido";
            }

        }
    }
}
