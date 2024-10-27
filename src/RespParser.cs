
namespace codecrafters_redis.src
{
    public class RespParser
    {
        private const string CLRF = "\r\n";

        public static string Decode(string respString)
        {
            switch (respString[0])
            {
                case '+':
                    return DecodeSimpleString(respString.Substring(1));
                
                case '$':
                    return DecodeBulkStrings(respString.Substring(1));
                case '*':
                    return DecodeArray(respString.Substring(1));
                default:
                    return "";
            }

        }

        private static string DecodeSimpleString(string input)
        {
            int crlfIndex = input.IndexOf(CLRF);
            if(crlfIndex ==-1){
                return "";
            }
            return input.Substring(0, crlfIndex);
        }

        private static string DecodeBulkStrings(string input){
            int crlfIndex = input.IndexOf(CLRF);
            if (crlfIndex == -1)
            {
                return "";
            }

            if(!int.TryParse(input.Substring(0, crlfIndex), out int length)){
                return "Invalid Bulk String";
            }

            int contentStart = crlfIndex + 2;

            if(input.Length < contentStart + length +2){
                return "Incomplete Bulk string";
            }

            if(input.Substring(contentStart+length, 2) != CLRF){
                return "Missing CLRF Terminator after bulk string";
            }

            return input.Substring(contentStart, length);
        }

        private static string DecodeArray(string input){
            int crlfIndex = input.IndexOf(CLRF);

            if(crlfIndex == -1){
                return "Missing array length terminator";
            }

            if(!int.TryParse(input.Substring(0, crlfIndex), out int length)){
                return "Invalid Array length";
            }

            int currentPosition = crlfIndex + 2;

            List<string> results = new List<string>();

            for(int i=0; i<length; i++){
                if(currentPosition >=input.Length){
                    return "Array data incomplete";
                }

                char type = input[currentPosition];

                int nextElement = FindNextElementEnd(input.Substring(currentPosition));

                if(nextElement ==-1){
                    return "Incomplete Array element";
                }

                string element = Decode(input.Substring(currentPosition, nextElement));

                results.Add(element);
                currentPosition += nextElement;
            }
            return string.Join(",", results);
        }

        private static int FindNextElementEnd(string input){
            char type = input[0];

            switch(type){
                case '$':
                    int firstCRLF = input.IndexOf(CLRF);
                    if (firstCRLF == -1) return -1;
                    if (!int.TryParse(input.Substring(1, firstCRLF - 1), out int length)) return -1;
                    return firstCRLF + 2 + length + 2;
                case '+':
                    int endIndex = input.IndexOf(CLRF);
                    return endIndex == -1 ? -1 : endIndex + 2;
                
                default:
                    return -1;
            }
        }
    }
}
