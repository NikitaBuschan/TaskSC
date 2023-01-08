using System.Xml.Linq;
using System.Net.Sockets;

// Server

XDocument xmlConfig;

int fromRange, toRange, tasksCount;

try
{
    xmlConfig = XDocument.Load("config.xml");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine(ex.Message + '\n');
    throw;
}

XElement? serverConfig = xmlConfig.Element("server");

if (serverConfig == null)
{
    Console.WriteLine("server object in config not found");
    return;
}

XElement? range = serverConfig.Element("range");

if (range == null)
{
    Console.WriteLine("range object in config is not found");
    return;
}

#region WithCheckingCorrectData

//// from Range

//XElement? fromRangeElement = range.Element("from");

//if (fromRangeElement == null)
//{
//    Console.WriteLine("from Range value in config is not found");
//    return;
//}

//if (int.TryParse(fromRangeElement.Value, out fromRange) == false)
//{
//    Console.WriteLine("fromRangeElement has wrong data type");
//    return;
//}

//// to Range

//XElement? toRangeElement = range.Element("to");

//if (toRangeElement == null)
//{
//    Console.WriteLine("to Range value in config is not found");
//    return;
//}

//if (int.TryParse(toRangeElement.Value, out toRange) == false)
//{
//    Console.WriteLine("toRangeElement has wrong data type");
//    return;
//}

//// Get tasks count

//XElement? tasksCountElement = serverConfig.Element("tasks");

//if (tasksCountElement == null)
//{
//    Console.WriteLine("tasks value in config is not found");
//    return;
//}

//if (int.TryParse(tasksCountElement.Value, out tasksCount) == false)
//{
//    Console.WriteLine("tasksCountElement has wrong data type");
//    return;
//}

#endregion

#region WithoutChecking

tasksCount = Convert.ToInt32(serverConfig.Element("tasks")?.Value);
fromRange = Convert.ToInt32(range.Element("from")?.Value);
toRange = Convert.ToInt32(range.Element("to")?.Value);

#endregion

int port = 4002;
string ipAddress = "127.0.0.1";

ulong packages = 0;

UdpClient server = new UdpClient(ipAddress, port);

var tasks = new List<Task>();

for (int i = 0; i < tasksCount; i++)
{
    tasks.Add(Task.Run(() => SendNumberAsync(fromRange, toRange)));
}

tasks.Add(Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(1000);

        byte[] data = BitConverter.GetBytes(packages);

        await server.SendAsync(data);
    }
}));

Console.ReadLine();

async Task SendNumberAsync(int fromRange, int toRange)
{
    while (true)
    {
        byte[] data = BitConverter.GetBytes(new Random().Next(fromRange, toRange));

        await server.SendAsync(data);

        packages++;
    }
}