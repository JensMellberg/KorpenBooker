using System.Net;
using System.Text.Json;

namespace KorpenBooker
{
    public class KorpenService
    {
        public static List<(long id, string date)> GetWorkouts(string fromDate, string toDate)
        {
            using var client = new HttpClient();
            var url = $"https://korpenstockholm.zoezi.se/api/public/workout/get/all?fromDate={fromDate}&toDate={toDate}";

            var response = client.GetStringAsync(url).Result;
            var parsedResponse = JsonSerializer.Deserialize<WorkoutResponse>(response);
            if (parsedResponse == null)
            {
                throw new Exception($"Couldn't parse {response}");
            }

            var validWorkouts = parsedResponse.workouts.Where(x => x.workoutType.name == "Bordtennis" && x.workoutType.allowBooking);
            return validWorkouts.Select(x => (x.id, x.startTime)).ToList();
        }

        public static void Book(string sessionToken, long workoutId, string userId)
        {
            using var client = ClientWithSessionCookie(sessionToken);
            var url = $"https://korpenstockholm.zoezi.se/api/memberapi/workoutBooking/add?workout={workoutId}&method=trainingcard&user_id={userId}";

            var response = client.PostAsync(url, null).Result;
            response.EnsureSuccessStatusCode();
        }

        public static (long id, string firstName, string lastName) GetUserId(string sessionToken)
        {
            using var client = ClientWithSessionCookie(sessionToken);
            var url = $"https://korpenstockholm.zoezi.se/api/memberapi/get/current";

            var response = client.GetStringAsync(url).Result;
            var parsedResponse = JsonSerializer.Deserialize<UserResponse>(response);
            if (parsedResponse == null)
            {
                throw new Exception($"Couldn't parse {response}");
            }

            return (parsedResponse.id, parsedResponse.firstname, parsedResponse.lastname);
        }

        private static HttpClient ClientWithSessionCookie(string sessionToken)
        {
            var cookies = new CookieContainer();

            cookies.Add(
                new Uri("https://korpenstockholm.zoezi.se"),
                new Cookie("session", sessionToken)
            );

            var handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                UseCookies = true
            };

            return new HttpClient(handler);
        }
    }

    public class WorkoutResponse
    {
        public Workout[] workouts { get; set; }
    }

    public class UserResponse
    {
        public long id { get; set; }

        public string firstname { get; set; }

        public string lastname { get; set; }
    }

    public class Workout
    {
        public long id { get; set; }

        public WorkoutType workoutType { get; set; }

        public string startTime { get; set; }
    }

    public class WorkoutType
    {
        public string name { get; set; }

        public bool allowBooking { get; set; }
    }
}
