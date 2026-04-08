using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

class Program
{
    static string HOST = "127.0.0.1";
    static int PORT = 9000;

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Použitie:");
            Console.WriteLine("list");
            Console.WriteLine("get <hash>");
            Console.WriteLine("upload <subor> <description>");
            Console.WriteLine("delete <hash>");
            return;
        }

        using (TcpClient client = new TcpClient(HOST, PORT))
        using (NetworkStream stream = client.GetStream())
        {
            string cmd = args[0];

            switch (cmd)
            {
                case "list":
                    CmdList(stream);
                    break;

                case "get":
                    CmdGet(stream, args[1]);
                    break;

                case "upload":
                    CmdUpload(stream, args[1], args[2]);
                    break;

                case "delete":
                    CmdDelete(stream, args[1]);
                    break;
            }
        }
    }

    static string ReadLine(NetworkStream stream)
    {
        MemoryStream ms = new MemoryStream();
        int b;

        while ((b = stream.ReadByte()) != -1)
        {
            if (b == '\n') break;
            ms.WriteByte((byte)b);
        }

        return Encoding.UTF8.GetString(ms.ToArray()).Trim();
    }

    static byte[] ReadExact(NetworkStream stream, int length)
    {
        byte[] buffer = new byte[length];
        int read = 0;

        while (read < length)
        {
            int r = stream.Read(buffer, read, length - read);
            if (r <= 0) break;
            read += r;
        }

        return buffer;
    }

    static void CmdList(NetworkStream stream)
    {
        Send(stream, "LIST\n");

        string header = ReadLine(stream);
        var parts = header.Split(' ');

        if (parts[0] != "200")
        {
            Console.WriteLine(header);
            return;
        }

        int count = int.Parse(parts[2]);

        for (int i = 0; i < count; i++)
        {
            Console.WriteLine(ReadLine(stream));
        }
    }

    static void CmdGet(NetworkStream stream, string hash)
    {
        Send(stream, $"GET {hash}\n");

        string header = ReadLine(stream);
        var parts = header.Split(' ');

        if (parts[0] != "200")
        {
            Console.WriteLine(header);
            return;
        }

        int length = int.Parse(parts[2]);

        string description = string.Join(" ", parts, 3, parts.Length - 3);

        byte[] data = ReadExact(stream, length);

        string filename = "down_" + description;
        File.WriteAllBytes(filename, data);

        Console.WriteLine($"Downloaded: {filename}");
    }

    static void CmdUpload(NetworkStream stream, string filepath, string description)
    {
        byte[] data = File.ReadAllBytes(filepath);
        int length = data.Length;

        Send(stream, $"UPLOAD {length} {description}\n");
        stream.Write(data, 0, data.Length);

        string response = ReadLine(stream);
        Console.WriteLine(response);
    }

    static void CmdDelete(NetworkStream stream, string hash)
    {
        Send(stream, $"DELETE {hash}\n");

        string response = ReadLine(stream);
        Console.WriteLine(response);
    }

    static void Send(NetworkStream stream, string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        stream.Write(data, 0, data.Length);
    }
}