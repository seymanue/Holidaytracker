
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PublicHolidayTracker
{
    public class Holiday
    {
        public string date { get; set; }
        public string localName { get; set; }
        public string name { get; set; }
        public string countryCode { get; set; }

        [JsonPropertyName("fixed")]
        public bool isFixed { get; set; }

        [JsonPropertyName("global")]
        public bool isGlobal { get; set; }

        [JsonIgnore]
        public DateTime Date
        {
            get
            {
                return DateTime.Parse(date);
            }
        }
    }

    class Program
    {
        private static readonly HttpClient http = new HttpClient();
        private static readonly Dictionary<int, List<Holiday>> cache = new();

        static async Task Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            await LoadYear(2023);
            await LoadYear(2024);
            await LoadYear(2025);

            while (true)
            {
                Console.WriteLine("\n===== PublicHolidayTracker =====");
                Console.WriteLine("1. Tatil listesini göster (yıl seç)");
                Console.WriteLine("2. Tarihe göre tatil ara (GG-AA)");
                Console.WriteLine("3. İsme göre tatil ara");
                Console.WriteLine("4. Tüm tatilleri göster (2023–2025)");
                Console.WriteLine("5. Çıkış");
                Console.Write("Seçim: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": await ShowByYear(); break;
                    case "2": SearchByDate(); break;
                    case "3": SearchByName(); break;
                    case "4": ShowAll(); break;
                    case "5": return;
                    default: Console.WriteLine("Geçersiz seçim."); break;
                }
            }
        }

        static async Task LoadYear(int year)
        {
            if (cache.ContainsKey(year)) return;

            string url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/TR";

            try
            {
                var json = await http.GetStringAsync(url);
                var holidays = JsonSerializer.Deserialize<List<Holiday>>(json);

                cache[year] = holidays ?? new List<Holiday>();
                Console.WriteLine($"{year} yılı yüklendi. ({cache[year].Count} adet tatil)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{year} yılı yüklenirken hata: {ex.Message}");
                cache[year] = new List<Holiday>();
            }
        }

        static async Task ShowByYear()
        {
            Console.Write("Yıl giriniz (2023–2025): ");
            var input = Console.ReadLine();

            if (!int.TryParse(input, out int year) || !cache.ContainsKey(year))
            {
                Console.WriteLine("Geçersiz yıl.");
                return;
            }

            var list = cache[year];
            if (!list.Any())
            {
                Console.WriteLine("Bu yıl için tatil bulunamadı.");
                return;
            }

            Console.WriteLine($"
--- {year} Tatilleri ---");
            foreach (var h in list.OrderBy(x => x.Date))
                Console.WriteLine($"{h.Date:dd-MM-yyyy} - {h.localName} ({h.name})");
        }

        static void SearchByDate()
        {
            Console.Write("Tarih (GG-AA): ");
            var input = Console.ReadLine();

            if (!TryParseDayMonth(input, out int day, out int month))
            {
                Console.WriteLine("Format hatalı.");
                return;
            }

            var results = cache.Values
                .SelectMany(x => x)
                .Where(h => h.Date.Day == day && h.Date.Month == month)
                .OrderBy(h => h.Date)
                .ToList();

            if (!results.Any())
            {
                Console.WriteLine("Bu tarihte tatil yok.");
                return;
            }

            Console.WriteLine($"
--- {input} Tatilleri ---");
            foreach (var h in results)
                Console.WriteLine($"{h.Date:dd-MM-yyyy} - {h.localName} ({h.name})");
        }

        static void SearchByName()
        {
            Console.Write("Aranan isim: ");
            string term = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(term))
            {
                Console.WriteLine("Boş olamaz.");
                return;
            }

            var results = cache.Values
                .SelectMany(x => x)
                .Where(h =>
                    h.localName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    h.name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderBy(h => h.Date)
                .ToList();

            if (!results.Any())
            {
                Console.WriteLine("Eşleşme bulunamadı.");
                return;
            }

            Console.WriteLine($"
--- Arama Sonuçları ({term}) ---");
            foreach (var h in results)
                Console.WriteLine($"{h.Date:dd-MM-yyyy} - {h.localName} ({h.name})");
        }

        static void ShowAll()
        {
            Console.WriteLine("
--- 2023–2025 Tüm Tatiller ---");

            var all = cache.Values.SelectMany(x => x).OrderBy(x => x.Date);

            foreach (var h in all)
                Console.WriteLine($"{h.Date:dd-MM-yyyy} - {h.localName} ({h.name})");
        }

        static bool TryParseDayMonth(string input, out int day, out int month)
        {
            day = month = 0;

            var parts = input?.Split('-');
            if (parts == null || parts.Length != 2) return false;

            bool ok1 = int.TryParse(parts[0], out day);
            bool ok2 = int.TryParse(parts[1], out month);

            return ok1 && ok2 && day is >= 1 and <= 31 && month is >= 1 and <= 12;
        }
    }
}
