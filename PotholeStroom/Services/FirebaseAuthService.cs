using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using PotholeStroom.Models;


namespace PotholeStroom.Services
{
    public class FirebaseAuthService
    {
        private readonly HttpClient _httpClient;

        public FirebaseAuthService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> RegisterUser(string email, string password, string userType)
        {
            var signupEndpoint = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseAuthConfig.WebApiKey}";

            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(signupEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Sign up failed: {responseContent}";
            }

            // Get UID (localId)
            var resultData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
            var localId = resultData["localId"];

            // Save email and userType to Realtime Database
            var userData = new { email, userType };
            var userJson = JsonConvert.SerializeObject(userData);
            

            var dbContent = new StringContent(userJson, Encoding.UTF8, "application/json");

            var idToken = resultData["idToken"];
            var databaseUrl = $"{FirebaseAuthConfig.DatabaseUrl}users/{localId}.json?auth={idToken}";

            var dbResponse = await _httpClient.PutAsync(databaseUrl, dbContent);


            if (!dbResponse.IsSuccessStatusCode)
            {
                return "User created but failed to save role.";
            }

            // Navigate to correct page based on role
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (userType == "Driver")
                {
                    Application.Current.MainPage = new NavigationPage(new DriverPage(idToken));
                }
                else if (userType == "Road Worker")
                {
                    Application.Current.MainPage = new NavigationPage(new RoadWorkerPage(localId, idToken));
                }
                else
                {
                    Application.Current.MainPage.DisplayAlert("Error", "Unknown user type.", "OK");
                }
            });

            return "Account created!";
        }



        public async Task<string> LoginUser(string email, string password)
        {
            var loginEndpoint = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseAuthConfig.WebApiKey}";

            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(loginEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Login failed: {responseContent}";
            }

            // ✅ Login succeeded, get UID (localId)
            var resultData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
            var localId = resultData["localId"];
            var idToken = resultData["idToken"];

            // ✅ Now get userType from Realtime Database
            var dbUrl = $"{FirebaseAuthConfig.DatabaseUrl}users/{localId}.json?auth={idToken}";
            var dbResponse = await _httpClient.GetAsync(dbUrl);

            if (!dbResponse.IsSuccessStatusCode)
            {
                return "Login succeeded, but failed to retrieve user data.";
            }

            var dbContent = await dbResponse.Content.ReadAsStringAsync();
            var userData = JsonConvert.DeserializeObject<Dictionary<string, string>>(dbContent);

            if (!userData.ContainsKey("userType"))
            {
                return "User type not found in database.";
            }

            var userType = userData["userType"];

            // Navigate to appropriate page on UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (userType == "Driver")
                {
                    Application.Current.MainPage = new NavigationPage(new DriverPage(idToken));
                }
                else if (userType == "Road Worker")
                {
                    Application.Current.MainPage = new NavigationPage(new RoadWorkerPage(localId, idToken));
                }
                else
                {
                    Application.Current.MainPage.DisplayAlert("Error", "Unknown user type.", "OK");
                }
            });

            return "Login successful!";
        }


    }
}
