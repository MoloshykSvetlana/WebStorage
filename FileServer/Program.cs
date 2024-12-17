using System.Text;
using FileServer;
using FileServer.Enums;
using FileServer.Models;
using Microsoft.EntityFrameworkCore;
using OclUdp.Sockets;

OclUdpListener listener = new(1488);

var optionBuilder = new DbContextOptionsBuilder<ApplicationContext>();
optionBuilder.UseSqlServer("Server=DESKTOP-A20G80A\\SQLEXPRESS;Database=WebStorage;Trusted_Connection=True;MultipleActiveResultSets=true; TrustServerCertificate=true");
var context = new ApplicationContext(optionBuilder.Options);

while (true)
{
    OclUdpClient client = await listener.AcceptOclUdpClientAsync();
    Task.Run(async () => await ProcessClient(client));
}

async Task ProcessClient(OclUdpClient client)
{
    OclUdpStream stream = client.GetStream(); // Получение потока для обмена данными с клиентом

    byte[] aT = new byte[1]; // Создание массива байтов длиной 1
    await stream.ReadAsync(aT, 0, 1);

    byte[] userId = new byte[1];
    await stream.ReadAsync(userId, 0, 1);

    byte[] fNameb = new byte[4];
    stream.Read(fNameb, 0, 4); // Чтение 4 байтов из потока в массив fNameb
    int flength = BitConverter.ToInt32(fNameb);
    byte[] fbytes = new byte[flength];
    stream.Read(fbytes, 0, flength); // Чтение flength байтов из потока в массив fbytes

    byte[] contentLengthBuffer = new byte[sizeof(int)];
    await stream.ReadAsync(contentLengthBuffer, 0, sizeof(int));

    int contentLength = BitConverter.ToInt32(contentLengthBuffer); // Преобразование массива contentLengthBuffer в целое число
    byte[] dataBuffer = new byte[contentLength];
    await stream.ReadAsync(dataBuffer, 0 , contentLength);

    ActionType actionType = (ActionType)aT[0]; // Преобразование aT[0] в тип ActionType
    
    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data.txt"); // Формирование пути к файлу "Data.txt"

    long p; //оффсет для сущности (оффсет - место начала записи файла)
    using (FileStream fstream = new FileStream(path, FileMode.OpenOrCreate)) //созд файл
    {
        // запись массива байтов в файл
        p = fstream.Seek(0, SeekOrigin.End);

        await fstream.WriteAsync(dataBuffer, 0, dataBuffer.Length); //запись в файл байтов
    }
    
    switch (actionType)
    {
        case ActionType.LoadFiles:
        {
            var item = new FileModel
            {
                Name = System.Text.Encoding.UTF8.GetString(fbytes), // Преобразование массива fbytes в строку
                ContentLength = dataBuffer.Length,
                Offset = p,
                UserModelId = userId[0],
                ShareToAll = 1
            };

            context.Files.Add(item);
            context.SaveChanges();  // Сохранение изменений в базе данных
            break;
        }
    }
}
// Метод ReadNextInt читает следующее целое число из массива байтов и обновляет смещение в массиве данных.
// Параметры:
// - dataBuffer: Массив байтов, из которого происходит чтение.
// - dataBufferOffset: Ссылка на переменную, содержащую текущее смещение в массиве данных.
// Возвращает прочитанное целое число.
int ReadNextInt(byte[] dataBuffer, ref int dataBufferOffset)
{
    byte[] intBytes = dataBuffer[dataBufferOffset..(dataBufferOffset += sizeof(int))]; // Извлекаем массив байтов размером sizeof(int) начиная с текущего смещения.
    return BitConverter.ToInt32(intBytes);
}

string ReadNextString(byte[] dataBuffer, ref int dataBufferOffset)
{
    int length = ReadNextInt(dataBuffer, ref dataBufferOffset);
    byte[] stringBytes = dataBuffer[dataBufferOffset..(dataBufferOffset += length)];
    return Encoding.UTF8.GetString(stringBytes);
}
// Метод WriteNextInt записывает следующее целое число в массив байтов и обновляет смещение в массиве данных.
// Параметры:
// - dataBuffer: Массив байтов, в который происходит запись.
// - dataBufferOffset: Ссылка на переменную, содержащую текущее смещение в массиве данных.
// - value: Целое число, которое нужно записать.
void WriteNextInt(byte[] dataBuffer, ref int dataBufferOffset, int value)
{
    byte[] intBytes = BitConverter.GetBytes(value);
    Buffer.BlockCopy(intBytes, 0, dataBuffer, dataBufferOffset, sizeof(int));
    dataBufferOffset += sizeof(int);
}
// Метод WriteNextString записывает следующую строку в массив байтов и обновляет смещение в массиве данных.
// Параметры:
// - dataBuffer: Массив байтов, в который происходит запись.
// - dataBufferOffset: Ссылка на переменную, содержащую текущее смещение в массиве данных.
// - value: Строка, которую нужно записать.
void WriteNextString(byte[] dataBuffer, ref int dataBufferOffset, string value)
{
    byte[] stringBytes = Encoding.UTF8.GetBytes(value);
    int length = stringBytes.Length;
    WriteNextInt(dataBuffer, ref dataBufferOffset, length);
    Buffer.BlockCopy(stringBytes, 0, dataBuffer, dataBufferOffset, length);
    dataBufferOffset += length;
}