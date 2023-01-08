using System.Net.Sockets;

// Client

string filePath = "nums.dat";

int port = 4002;

UdpClient client = new UdpClient(port);

long gotPackages = 0;

var possibleToWrite = true;
var fileClosed = false;

var tempArray = new List<int>();

Task.Run(ReceiveMessageAsync).ConfigureAwait(false).GetAwaiter();

await Task.Run(ConsoleController);

async Task ReceiveMessageAsync()
{
    var file = File.Open(filePath, FileMode.OpenOrCreate);
    var binaryWriter = new BinaryWriter(file);

    while (true)
    {
        var result = await client.ReceiveAsync();

        // getting data on the number of sent packages
        if (result.Buffer.Length == 8)
        {
            Console.WriteLine($"gett: {BitConverter.ToUInt64(result.Buffer, 0)}");

            gotPackages = BitConverter.ToInt64(result.Buffer, 0);

            continue;
        }

        var receiveNum = BitConverter.ToInt32(result.Buffer, 0);

        if (possibleToWrite == false)
        {
            if (fileClosed == false)
            {
                // close connection with file
                file.Close();
                binaryWriter.Close();

                fileClosed = true;
            }

            // save to temp array
            tempArray.Add(receiveNum);
            continue;
        }

        if (tempArray.Count > 0)
        {
            // connection to file
            file = File.Open(filePath, FileMode.OpenOrCreate);
            binaryWriter = new BinaryWriter(file);

            // write all data from temp array to file
            foreach (var num in tempArray)
            {
                binaryWriter.Write(num);
            }

            tempArray = new List<int>();
        }

        // write data to file
        binaryWriter.Write(receiveNum);
    }

    file.Close();
    binaryWriter.Close();
}

Task ConsoleController()
{
    while (true)
    {
        var key = Console.ReadKey();

        if (key.Key == ConsoleKey.Enter)
        {
            var start = DateTime.UtcNow;

            // block write to file
            possibleToWrite = false;

            var packs = gotPackages;

            while (true)
            {
                if (fileClosed)
                {
                    var file = File.Open(filePath, FileMode.Open);
                    var binaryReader = new BinaryReader(file);

                    long sum = 0;
                    long numsCount = 0;

                    Dictionary<int, ulong> numsMode = new Dictionary<int, ulong>();

                    // read data from file
                    while (binaryReader.PeekChar() > -1)
                    {
                        int num = binaryReader.ReadInt32();

                        // building dictionary with numbers and count of repeats this num
                        if (numsMode.TryAdd(num, 1) == false)
                        {
                            numsMode[num]++;
                        }

                        sum += num;
                        numsCount++;
                    }

                    // find median
                    var medianFileCursorPostion = (numsCount / 2 - 1) * sizeof(int);

                    file.Position = medianFileCursorPostion;

                    var median = binaryReader.ReadInt32();

                    // getting average value
                    var average = sum / numsCount;

                    int modeNumber = 0;
                    ulong modeCount = 0;

                    double squareSum = 0;

                    // prepair data to calc standard deviation and find mode
                    Parallel.ForEach(numsMode, num =>
                    {
                        var diff = num.Value - (ulong)average;

                        var square = Math.Pow(diff, 2);

                        squareSum += square;

                        // find mode
                        if (num.Value > modeCount)
                        {
                            modeNumber = num.Key;
                            modeCount = num.Value;
                        }
                    });

                    // calc standard deviation
                    ulong zeros = 0;

                    if (numsMode.TryGetValue(0, out ulong value) == true)
                    {
                        zeros = numsMode[0];
                    }

                    var res = squareSum / (numsCount - (long)zeros) - 1;

                    var deviation = Math.Sqrt(res);

                    Console.WriteLine($"Count: {numsCount}");
                    Console.WriteLine($"Average: {average}");
                    Console.WriteLine($"Packs: {packs}");
                    Console.WriteLine($"Loss: {packs - numsCount}");
                    Console.WriteLine($"Mod: {modeNumber}");
                    Console.WriteLine($"Deviation: {deviation}");
                    Console.WriteLine($"Median: {median}");
                    Console.WriteLine();

                    file.Close();
                    binaryReader.Close();

                    possibleToWrite = true;
                    fileClosed = false;
                    break;
                }
            }

            Console.WriteLine($"calc time: {DateTime.UtcNow - start}\n\n");
        }
        else if (key.Key == ConsoleKey.Escape)
        {
            Environment.Exit(0);
        }
    }
}