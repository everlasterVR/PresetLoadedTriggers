using SimpleJSON;
using System.Text;

namespace everlaster
{
    static class JSONUtils
    {
        public static string Prettify(JSONNode jsonNode)
        {
            var sb = new StringBuilder();
            string json = jsonNode.ToString();
            json = json.Replace(", ", ","); // prevent extra space after line break added
            int indentLevel = 0;
            int jsonLength = json.Length;
            bool inQuotes = false;

            for(int i = 0; i < jsonLength; i++)
            {
                char currentChar = json[i];
                switch(currentChar)
                {
                    case '{':
                    case '[':
                        sb.Append(currentChar);
                        if(!inQuotes)
                        {
                            indentLevel++;
                            AppendIndentedLine(sb, indentLevel);
                        }

                        break;
                    case '}':
                    case ']':
                        if(!inQuotes)
                        {
                            indentLevel--;
                            AppendIndentedLine(sb, indentLevel);
                        }

                        sb.Append(currentChar);
                        break;
                    case ',':
                        sb.Append(currentChar);
                        if(!inQuotes)
                        {
                            AppendIndentedLine(sb, indentLevel);
                        }

                        break;
                    case ':':
                        sb.Append(currentChar);
                        if(!inQuotes)
                        {
                            sb.Append(" ");
                        }

                        break;
                    case '\"':
                        if(i > 0 && json[i - 1] != '\\')
                        {
                            inQuotes = !inQuotes;
                        }

                        sb.Append(currentChar);
                        break;
                    default:
                        sb.Append(currentChar);
                        break;
                }
            }

            return sb.ToString();
        }

        static void AppendIndentedLine(StringBuilder sb, int indentLevel) =>
            sb.AppendFormat("\n{0}", new string(' ', indentLevel * 4));
    }
}
