using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32.TaskScheduler;

namespace KorpenBooker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1 && args[0] == "-book")
            {
                Book(args[1]);
                return;
            }

            var workoutDates = new Dictionary<long, string>();
            var storage = Storage.GetFromFile();
            Console.WriteLine("Tjo!");
            if (string.IsNullOrEmpty(storage.SessionCookie))
            {
                Console.WriteLine("Du har inte fyllt i sessionskakan, gör det genom -session <cookie-value>");
            }

            if (string.IsNullOrEmpty(storage.UserId))
            {
                Console.WriteLine("Du har inte fyllt i ditt user id, gör det genom -userid <userid> eller uppdatera sessionskakan så görs ett automatiskt anrop");
            }

            Console.WriteLine("För att se id'n på träningspass. Kör -list <frånDatum> <tillDatum>");
            Console.WriteLine("Datum är på formatet YYYY-MM-DD");
            while (true)
            {
                var command = Console.ReadLine();
                var tokens = command?.Split(' ');
                if (command?.StartsWith("-list") == true)
                {
                    if (tokens!.Length != 3)
                    {
                        Console.WriteLine("Behöver start och slutdatum på (YYYY-MM-DD)");
                        continue;
                    }

                    var fromDate = tokens[1];
                    var toDate = tokens[2];
                    if (!IsDate(fromDate) || !IsDate(toDate))
                    {
                        continue;
                    }

                    List<(long id, string date)> workouts;
                    try
                    {
                        workouts = KorpenService.GetWorkouts(fromDate, toDate);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error vid hämtande av träningspass: {e.Message}");
                        continue;
                    }
                    
                    Console.WriteLine("Alla pingisträningar mellan datumen: ");
                    foreach (var workout in workouts)
                    {
                        Console.WriteLine($"{workout.date}: träningsid: {workout.id}");
                        workoutDates.TryAdd(workout.id, workout.date);
                    }

                    Console.WriteLine("För att boka ett pass när bokningen öppnar. Kör -book <träningsid>");
                }
                else if (command?.StartsWith("-userid") == true)
                {
                    if (tokens!.Length != 2)
                    {
                        Console.WriteLine("Behöver ett userid");
                        continue;
                    }

                    storage.UserId = tokens[1];
                    storage.SaveToFile();
                    Console.WriteLine($"Userid uppdaterat till {storage.UserId}");
                }
                else if (command?.StartsWith("-session") == true)
                {
                    if (tokens!.Length != 2)
                    {
                        Console.WriteLine("Behöver en sessionskaka");
                        continue;
                    }

                    storage.SessionCookie = tokens[1];
                    storage.SaveToFile();
                    Console.WriteLine($"Sessionskakan uppdaterat till {storage.SessionCookie}. Försöker hämta användarid");

                    try
                    {
                        var (userId, firstName, lastName) = KorpenService.GetUserId(tokens[1]);
                        Console.WriteLine($"Användaruppgifter hämtade! Tjena {firstName} {lastName}. Ditt id är {userId}");
                        storage.UserId = userId.ToString();
                        storage.SaveToFile();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error vid hämtande av användarid: {e.Message}");
                    }
                }
                else if (command?.StartsWith("-book") == true)
                {
                    if (tokens!.Length != 2)
                    {
                        Console.WriteLine("Behöver ett id till träningen");
                        continue;
                    }

                    if (!long.TryParse(tokens[1], out var workoutId))
                    {
                        Console.WriteLine("Id't måste vara ett nummer");
                        continue;
                    }

                    if (!workoutDates.TryGetValue(workoutId, out var workoutDate))
                    {
                        Console.WriteLine("Programmet minns inte datumet för träningen, kör en -list igen så sparas det ner");
                        continue;
                    }

                    if (string.IsNullOrEmpty(storage.SessionCookie))
                    {
                        Console.WriteLine("Du behöver ange en sessionskaka för att kunna boka ett pass");
                        continue;
                    }

                    if (string.IsNullOrEmpty(storage.UserId))
                    {
                        Console.WriteLine("Du behöver ange ett userid för att kunna boka ett pass");
                        continue;
                    }

                    ScheduleBooking(workoutDate, workoutId);
                }
                else if (command?.Equals("exit") == true)
                {
                    return;
                }
                else
                {
                    Console.WriteLine($"Okänt command: {command}");
                }
            }
        }

        static void Book(string workoutId)
        {
            var storage = Storage.GetFromFile();
            var log = new StringBuilder();
            log.AppendLine($"Försöker boka korpenträning. Tid: {DateTime.Now}");
            if (string.IsNullOrEmpty(storage.SessionCookie))
            {
                log.AppendLine("Finns ingen sessionskaka, avbryter");
                WriteLog();
                return;
            }

            if (string.IsNullOrEmpty(storage.UserId))
            {
                log.AppendLine("Finns inget användarid, avbryter");
                WriteLog();
                return;
            }

            PerformBookingWithRetries(() => KorpenService.Book(storage.SessionCookie, long.Parse(workoutId), storage.UserId), log, 1);
            WriteLog();

            void WriteLog()
            {
                var logFileName = $"log{DateTime.Now.ToString()[..10]}.txt";
                var filePath = Path.Combine(AppContext.BaseDirectory, logFileName);
                File.WriteAllText(filePath, log.ToString());

                using var ts = new TaskService();
                ts.RootFolder.DeleteTask($"Korpen-bokning-{workoutId}", false);
            }
        }

        static void PerformBookingWithRetries(System.Action book, StringBuilder log, int tryCount)
        {
            const int maxTries = 2;
            try
            {
                book();
                log.AppendLine($"Bokat! {DateTime.Now}");
            }
            catch (Exception e)
            {
                if (tryCount < maxTries)
                {
                    log.AppendLine($"Error vid bokningsanropet: {e.Message}, försöker igen om 2 sekunder");
                    Thread.Sleep(2000);
                    PerformBookingWithRetries(book, log, tryCount + 1);
                }
                else
                {
                    log.AppendLine($"Error vid bokningsanrop {tryCount}: {e.Message}, avbryter");
                }
            }
        }

        static void ScheduleBooking(string date, long workoutId)
        {
            var bookDate = GetDateTimeToBook(date);
            if (bookDate < DateTime.Now)
            {
                Book(workoutId.ToString());
                return;
            }

            var exePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Failed to find path for .exe-file");
            using var ts = new TaskService();

            var task = ts.NewTask();

            task.RegistrationInfo.Description = "Boka korpen";

            task.Triggers.Add(new TimeTrigger
            {
                StartBoundary = bookDate
            });

            task.Actions.Add(new ExecAction(
                exePath,
                $"-book {workoutId}",
                null));

            task.Settings.WakeToRun = true;

            ts.RootFolder.RegisterTaskDefinition(
                $"Korpen-bokning-{workoutId}",
                task);

            Console.WriteLine($"Bokningen kommer göras {bookDate.ToString().Substring(0, 10)} kl 07:00. Tryck WIN + R och sen kör taskschd.msc och ta bort aktiviteten om du ångrar dig");
        }

        static bool IsDate(string date)
        {
            var isDate = Regex.IsMatch(date, @"^\d{4}-\d{2}-\d{2}$");
            if (!isDate)
            {
                Console.WriteLine($"Datum måste vara på formen YYYY-MM-DD, det här funkar inte sörru {date}");
            }

            return isDate;
        }

        static DateTime GetDateTimeToBook(string date)
        {
            var year = date[..4];
            var month = date.Substring(5, 2);
            var day = date.Substring(8, 2);
            var workoutDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day), 7, 0, 0);
            return workoutDate.AddDays(-5);
        }
    }
}
