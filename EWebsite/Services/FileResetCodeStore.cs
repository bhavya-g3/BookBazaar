using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace EWebsite.Services
{
    public interface IResetCodeStore
    {
        Task<string> GenerateAndLogAsync(string email, HttpContext? httpContext = null, TimeSpan? ttl = null);
        Task<bool> ValidateAsync(string email, string code);
    }
    public sealed class FileResetCodeStore : IResetCodeStore
    {
        private readonly string _primaryFolder;
        private readonly string _desktopFallback;
        private readonly string _localAppDataFallback;
        private readonly object _lock = new();

        private string Folder { get; set; } = string.Empty;

        public FileResetCodeStore(IOptions<Options> options)
        {
            _primaryFolder = options.Value.Folder?.Trim() ?? string.Empty;

            _desktopFallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Logs");

            _localAppDataFallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EWebsite",
                "Logs");

            Folder = EnsureAnyWritableFolder();
        }

        public class Options
        {
            public string Folder { get; set; } = string.Empty;
        }

        // -----------------------------------------------------------
        // Generate code + write log
        // -----------------------------------------------------------
        public Task<string> GenerateAndLogAsync(string email, HttpContext? httpContext = null, TimeSpan? ttl = null)
        {
            var now = DateTimeOffset.UtcNow;

            var code = RandomNumberGenerator.GetInt32(10000, 1_000_000)
                .ToString(CultureInfo.InvariantCulture);

            var expires = now.Add(ttl ?? TimeSpan.FromMinutes(10));

            // format: UTC ISO | email | code | expiry
            var line = $"{now:o}|{email}|{code}|{expires:o}";

            var foldersToTry = new[]
            {
                _primaryFolder,
                _desktopFallback,
                _localAppDataFallback,
                ResolveWritableFolder()
            };

            bool wrote = false;

            foreach (var folder in foldersToTry)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;

                try
                {
                    Directory.CreateDirectory(folder);

                    var path = GetTodayLogPath(folder);

                    lock (_lock)
                    {
                        File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                    }

                    wrote = true;
                    break;
                }
                catch
                {
                }
            }

            if (!wrote)
            {
                try
                {
                    var temp = Path.Combine(Path.GetTempPath(), "EWebsite", "Logs");
                    Directory.CreateDirectory(temp);

                    var path = GetTodayLogPath(temp);

                    lock (_lock)
                    {
                        File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                    }

                    wrote = true;
                }
                catch
                {
                }
            }

            return Task.FromResult(code);
        }

        // -----------------------------------------------------------
        // Validate code
        // -----------------------------------------------------------
        public Task<bool> ValidateAsync(string email, string code)
        {
            var folders = new[]
            {
                _primaryFolder,
                _desktopFallback,
                _localAppDataFallback,
                ResolveWritableFolder()
            };

            var dates = new[]
            {
                DateTime.UtcNow.Date,
                DateTime.UtcNow.AddDays(-1).Date
            };

            string? latest = null;

            lock (_lock)
            {
                foreach (var folder in folders)
                {
                    if (string.IsNullOrWhiteSpace(folder))
                        continue;

                    foreach (var date in dates)
                    {
                        var path = Path.Combine(folder, $"PasswordReset_{date:yyyy-MM-dd}.log");
                        if (!File.Exists(path))
                            continue;

                        try
                        {
                            foreach (var line in File.ReadLines(path, Encoding.UTF8))
                            {
                                var parts = line.Split('|');
                                if (parts.Length == 4 && parts[1].Equals(email, StringComparison.OrdinalIgnoreCase))
                                {
                                    latest = line;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (latest == null)
                return Task.FromResult(false);

            var p = latest.Split('|');
            var storedCode = p[2];

            if (!DateTimeOffset.TryParse(p[3], null, DateTimeStyles.RoundtripKind, out var expiry))
                return Task.FromResult(false);

            var ok = storedCode == code && DateTimeOffset.UtcNow <= expiry;
            return Task.FromResult(ok);
        }

        // -----------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------
        private string ResolveWritableFolder() => EnsureAnyWritableFolder();

        private string GetTodayLogPath(string folder) =>
            Path.Combine(folder, $"PasswordReset_{DateTime.UtcNow:yyyy-MM-dd}.log");

        private string EnsureAnyWritableFolder()
        {
            if (TryEnsureDirectory(_primaryFolder, out var ok) && ok)
                return _primaryFolder;

            if (TryEnsureDirectory(_desktopFallback, out ok) && ok)
                return _desktopFallback;

            if (TryEnsureDirectory(_localAppDataFallback, out ok) && ok)
                return _localAppDataFallback;

            return _localAppDataFallback; 
        }

        private static bool TryEnsureDirectory(string? folder, out bool createdOrExists)
        {
            createdOrExists = false;

            try
            {
                if (string.IsNullOrWhiteSpace(folder))
                    return false;

                Directory.CreateDirectory(folder);
                createdOrExists = true;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}