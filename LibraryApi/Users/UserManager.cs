using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace LibraryApi.Users
{
    public class UserManager
    {
        private readonly HttpClient _httpClient;

        public UserManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Prediction> CheckUserPhoto(string imageBase64)
        {
            var response = await _httpClient.PostAsJsonAsync("facial", new
            {
                image = imageBase64
            });

            response.EnsureSuccessStatusCode();

            var prediction = JsonConvert.DeserializeObject<Prediction>(await response.Content.ReadAsStringAsync());
            return prediction;
        }

        public async Task AddPhotoToFacialRec(string name, string imageBase64)
        {
            var response = await _httpClient.PostAsJsonAsync("facial/add", new
            {
                name = name,
                image = imageBase64
            });

            response.EnsureSuccessStatusCode();
        }
    }

    public class Prediction
    {
        public string ClassName { get; set; }
        public decimal Distance { get; set; }
    }
}
