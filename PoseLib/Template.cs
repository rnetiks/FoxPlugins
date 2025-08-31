using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace PoseLib.KKS
{
    public class Template
    {
        private string _value;
        [CanBeNull] private string _workingDirectory;
        private static Random _random = new Random();
        private bool _createDirectories;

        public Template(string value, [CanBeNull] string workingDirectory = null, bool createDirectories = false)
        {
            _value = value;
            if (workingDirectory == null)
            {
                _workingDirectory = Directory.GetCurrentDirectory();
            }
            else
            {
                _workingDirectory = workingDirectory;
            }
            _createDirectories = createDirectories;
        }

        public void Compile()
        {
            _value = CompilePathTemplate(_value);
        }

        private string CompilePathTemplate(string templatePath)
        {
            string normalizedPath = templatePath.Replace('\\', '/');
            var segments = normalizedPath.Split('/');
            var resolvedSegments = new string[segments.Length];
            
            bool isAbsolute = Path.IsPathRooted(templatePath);
            
            string currentPath = isAbsolute ? "" : _workingDirectory;

            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                
                if (string.IsNullOrEmpty(segment))
                {
                    resolvedSegments[i] = segment;
                    continue;
                }

                string resolvedSegment = EvaluateNestedExpressions(segment, currentPath);
                resolvedSegments[i] = resolvedSegment;

                if (isAbsolute && string.IsNullOrEmpty(currentPath))
                {
                    currentPath = resolvedSegment;
                }
                else
                {
                    currentPath = Path.Combine(currentPath, resolvedSegment);
                }

                if (_createDirectories && i < segments.Length - 1 && !Directory.Exists(currentPath))
                {
                    Directory.CreateDirectory(currentPath);
                }
            }

            string result = "";
            foreach (string segment in resolvedSegments)
            {
                if (string.IsNullOrEmpty(segment))
                    continue;

                result = string.IsNullOrEmpty(result) ? segment : Path.Combine(result, segment);
            }
            return result;
        }

        private string EvaluateNestedExpressions(string input, string contextDir)
        {
            string result = input;
            int maxDepth = 10;
            int depth = 0;

            while (depth < maxDepth)
            {
                string before = result;
                result = ProcessSingleLevel(result, contextDir);

                if (result == before)
                    break;

                depth++;
            }

            return result;
        }

        private string ProcessSingleLevel(string input, string contextDir)
        {
            var pattern = @"\$\{([^{}$]+)\}";
            
            return Regex.Replace(input, pattern, match =>
            {
                string expression = match.Groups[1].Value;
                string evaluated = EvaluateExpression(expression, contextDir);
                return evaluated;
            });
        }

        private string EvaluateExpression(string expression, string contextDir)
        {
            var nestedParenMatch = Regex.Match(expression, @"^([A-Za-z_]+)\((.+)\)$");
            if (nestedParenMatch.Success)
            {
                string functionName = nestedParenMatch.Groups[1].Value;
                string args = nestedParenMatch.Groups[2].Value;
                
                string processedArgs = EvaluateNestedExpressions(args, contextDir);
                return EvaluateFunction(functionName, processedArgs, contextDir);
            }

            var colonMatch = Regex.Match(expression, @"^([A-Za-z_]+):(.+)$");
            if (colonMatch.Success)
            {
                string functionName = colonMatch.Groups[1].Value;
                string args = colonMatch.Groups[2].Value;
                return EvaluateFunction(functionName, args, contextDir);
            }


            return EvaluateBuiltinExpression(expression, contextDir);
        }

        private string EvaluateFunction(string functionName, string args, string contextDir)
        {
            switch (functionName.ToUpper())
            {
                case "UPPER":
                    return args.ToUpper();
                case "LOWER":
                    return args.ToLower();
                case "TRIM":
                    return args.Trim();
                case "REVERSE":
                    return new string(args.Reverse().ToArray());
                case "RANDOMTEXT":
                    return EvaluateRandomText(args);
                case "RANDOMNUMBER":
                    return EvaluateRandomNumber(args);
                case "REPEAT":
                    return EvaluateRepeat(args);
                case "SUBSTRING":
                    return EvaluateSubstring(args);
                default:
                    return EvaluateBuiltinExpression($"{functionName}:{args}", contextDir);
            }
        }

        private string EvaluateRandomText(string args)
        {
            if (int.TryParse(args, out int length))
                return GenerateRandomText(length);
            return args;
        }

        private string EvaluateRandomNumber(string args)
        {
            if (int.TryParse(args, out int max))
                return _random.Next(0, max).ToString();
            return args;
        }

        private string EvaluateRepeat(string args)
        {
            var parts = args.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int count))
                return string.Concat(Enumerable.Repeat(parts[0].Trim(), count));
            return args;
        }

        private string EvaluateSubstring(string args)
        {
            var parts = args.Split(',');
            if (parts.Length < 2 || !int.TryParse(parts[1].Trim(), out int start))
                return args;

            string text = parts[0].Trim();
            if (text.Length <= start)
                return "";

            if (parts.Length >= 3 && int.TryParse(parts[2].Trim(), out int length))
                return text.Substring(start, Math.Min(length, text.Length - start));
            
            return text.Substring(start);
        }

        private string EvaluateBuiltinExpression(string expression, string contextDir)
        {
            switch (expression.ToUpper())
            {
                case "CURRENTDIR":
                    return contextDir;
                case "DIRCOUNT":
                    return GetDirectoryCount(contextDir).ToString();
                case "FILECOUNT":
                    return GetFileCount(contextDir, "*").ToString();
                case "DATE":
                    return DateTime.Now.ToString("yyyy-MM-dd");
                case "TIME":
                    return DateTime.Now.ToString("HH.mm.ss");
                default:
                    return EvaluateComplexExpression(expression, contextDir);
            }
        }

        private string EvaluateComplexExpression(string expression, string contextDir)
        {
            var dirCountMatch = Regex.Match(expression, @"^DIRCOUNT:(\d+)$", RegexOptions.IgnoreCase);
            if (dirCountMatch.Success)
            {
                int padding = int.Parse(dirCountMatch.Groups[1].Value);
                return GetDirectoryCount(contextDir).ToString($"D{padding}");
            }

            var dirExistsMatch = Regex.Match(expression, @"^DIREXISTS:(.+)$", RegexOptions.IgnoreCase);
            if (dirExistsMatch.Success)
            {
                string dir = dirExistsMatch.Groups[1].Value;
                return Directory.Exists(Path.Combine(contextDir, dir)) ? "1" : "0";
            }

            var fileCountMatch = Regex.Match(expression, @"^FILECOUNT:(\d+)$", RegexOptions.IgnoreCase);
            if (fileCountMatch.Success)
            {
                int padding = int.Parse(fileCountMatch.Groups[1].Value);
                return GetFileCount(contextDir, "*").ToString($"D{padding}");
            }

            var fileCountPatternMatch = Regex.Match(expression, @"^FILECOUNT:(\d+):(.+)$", RegexOptions.IgnoreCase);
            if (fileCountPatternMatch.Success)
            {
                int padding = int.Parse(fileCountPatternMatch.Groups[1].Value);
                string pattern = fileCountPatternMatch.Groups[2].Value;
                return GetFileCount(contextDir, pattern).ToString($"D{padding}");
            }

            var fileExistsMatch = Regex.Match(expression, @"^FILEEXISTS:(.+)$", RegexOptions.IgnoreCase);
            if (fileExistsMatch.Success)
            {
                string file = fileExistsMatch.Groups[1].Value;
                return File.Exists(Path.Combine(contextDir, file)) ? "1" : "0";
            }

            var dateMatch = Regex.Match(expression, @"^DATE:(.+)$", RegexOptions.IgnoreCase);
            if (dateMatch.Success)
            {
                string format = dateMatch.Groups[1].Value;
                return DateTime.Now.ToString(format);
            }

            return expression;
        }

        private int GetDirectoryCount(string path)
        {
            try
            {
                bool exists = Directory.Exists(path);
                int count = exists ? Directory.GetDirectories(path).Length : 0;
                
                return count;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private int GetFileCount(string path, string pattern = "*")
        {
            try
            {
                bool exists = Directory.Exists(path);
                int count = exists ? Directory.GetFiles(path, pattern).Length : 0;
                
                return count;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private string GenerateRandomText(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[_random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        public string Value => _value;
        public string WorkingDirectory => _workingDirectory;
        public override string ToString() => _value;
    }
}