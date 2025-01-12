using System;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using TP;

class Program
{
    static void Main(string[] args)
    {
        var serverAddress = "127.0.0.1"; // Ersetze dies durch die Server-IP
        var port = 9901;
        var stir = new SendTournamentInfoRequest()
        {
            Password = "asdf",
            IP = serverAddress
        };

        StringBuilder sb = new StringBuilder();
        using (StringWriter ss = new StringWriter(sb))
        using (var xmlWriter = XmlWriter.Create(ss))
        {
            stir.CreateDocument().WriteContentTo(xmlWriter);
        }
        string textToSend = sb.ToString();
        try
        {
            // Text komprimieren
            byte[] compressedData = CompressWithGzip(textToSend);

            // TCP-Verbindung herstellen und Daten senden
            using (TcpClient client = new TcpClient(serverAddress, port))
            using (NetworkStream stream = client.GetStream())
            {
                // Daten senden
                stream.Write(compressedData, 0, compressedData.Length);
                Console.WriteLine("Daten wurden gesendet.");

                // Antwort empfangen
                using (MemoryStream responseStream = new MemoryStream())
                {
                    byte[] buffer = new byte[102400];
                    int bytesRead;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        responseStream.Write(buffer, 0, bytesRead);
                    }

                    // Antwort dekodieren und ggf. entpacken
                    byte[] responseData = responseStream.ToArray();
                    string responseText = DecompressWithGzip(responseData);

                    // Antwort ausgeben
                    Console.WriteLine("Antwort vom Server:");
                    Console.WriteLine(responseText);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler: {ex.Message}");
        }
    }

    static byte[] CompressWithGzip(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);

        using (MemoryStream compressedStream = new MemoryStream())
        {
            using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return compressedStream.ToArray();
        }
    }

    static string DecompressWithGzip(byte[] compressedData)
    {
        using (MemoryStream compressedStream = new MemoryStream(compressedData))
        using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        using (MemoryStream decompressedStream = new MemoryStream())
        {
            gzipStream.CopyTo(decompressedStream);
            return Encoding.UTF8.GetString(decompressedStream.ToArray());
        }
    }
}
