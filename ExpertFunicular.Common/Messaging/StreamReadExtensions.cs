using System.IO;

namespace ExpertFunicular.Common.Messaging;

public static class StreamReadExtensions
{
    public static void ReadMessage(this Stream stream)
    {
        // Headers array (...\n...\n)
        // EoF
        // \n
        // Message raw = ... ContentLength header
        // \n
        // EoF
        // \n
    }
}