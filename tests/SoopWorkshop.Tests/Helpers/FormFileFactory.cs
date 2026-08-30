using System.Text;
using Microsoft.AspNetCore.Http;

namespace SoopWorkshop.Tests.Helpers
{
    // Erzeugt IFormFile-Instanzen für die Upload-Validierung.
    public static class FormFileFactory
    {
        public static IFormFile Create(string fileName, string content = "public class Main {}")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "files", fileName);
        }

        // Datei mit vorgegebener Größe, ohne dafür echten Inhalt zu erzeugen.
        public static IFormFile CreateWithSize(string fileName, long lengthInBytes)
        {
            var stream = new MemoryStream();
            return new FormFile(stream, 0, lengthInBytes, "files", fileName);
        }
    }
}
