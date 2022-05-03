using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace MorskojBoj
{
    public partial class Form1 : Form
    {
        //массивы игровых полей
        Random rnd = new Random();  //рандом генератор для генерации кораблей
        int[,] mypole = new int[12, 12];  //матрица моего поля
        int[,] enemypole = new int[12, 12];  //матрица вражьего поля
        int[,] atacking = new int[12, 12];   //матрица для аттаки 

        bool step = false;   //определяет есть ход или нет
        string mypolesend;     //сюда матрица будет записываться в строку для дальнейшей отправки
        Graphics gr;         // холст для рисования
        int balls = 0;      //счёт игры


        public Form1()
        {
            InitializeComponent();
            gr = this.CreateGraphics();

            timer1.Interval = 10;           //интервал таймера
            timer1.Tick += timer1_Tick;     //инкрементация таймера 

            ipPoint = new IPEndPoint(IPAddress.Parse(textBox1.Text),port1);  // отвечает за подключения к другому компьютеру 
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  //определяет протокол TCP в сокете
        }

        //Сетевые сокеты
        public IPEndPoint ipPoint;
        public Socket listenSocket;
        public Socket handler;

        public int port1 = 3300;  //адрес Порта

        //Функция для установления соединения между сервером и клиентом
        public void Connect()
        {
            if (radioButton1.Checked == true)  //играем за сервер
            {
                listenSocket.Bind(ipPoint); //Связывает объект Socket с локальной конечной точкой.
                listenSocket.Listen(10);      //Ожидание принятия соединения          
                handler = listenSocket.Accept(); //зависаем до соединения клиента


                step = false;  //шага нет 
                atack(0, 0, handler);    //вызов главной функции для присваения в неё сокета handler - точнее зависания до ожидания клиента
            }
            else  //играем за клиент 
            {
                listenSocket.Connect(ipPoint);  //подключение к хосту
                step = true;                    //ход есть
            }
            connecting = true;  //соединение есть
        }


        byte[] data = new byte[256];  // массив байт для передачи
        StringBuilder builder = new StringBuilder(); // изменяемая строка символов
        Socket SockettoTimer;  //сокет для таймера
        public void atack(int X, int Y, Socket SendSocket)  //функция отправки и приёма координат и их анализа 
        {
            SockettoTimer = SendSocket;
            if (step == false)  //не ходим
            {
                toolStripStatusLabel4.Text = "Ожидание";
                int bytes = SendSocket.Receive(data, data.Length, 0);                   //зависаем, пока не придут данные от клиента
                X = Convert.ToInt32(Encoding.Unicode.GetString(data, 0, bytes));  // добавляем в конец строки Builder полученные данные
                bytes = SendSocket.Receive(data, data.Length, 0);              // данные data записываются в байт, после получения сервер развисает
                Y = Convert.ToInt32(Encoding.Unicode.GetString(data, 0, bytes));  // добавляем в конец строки Builder полученные данные


                if (mypole[X + 1, Y + 1] == 1)   //если в ячейке массива по полученым координатам, есть кораблик, то отправляем единицу
                {
                    data = Encoding.Unicode.GetBytes("1"); //кодирование сообщения о победе
                    SendSocket.Send(data); //отправка сообщения о победе
        
                    mypole[X + 1, Y + 1] = 2;   //Заменяем в поле того, по кому стреляли, кораблик на подбитый кораблик
                    timer1.Start();                    
                }
                else
                {                   
                    step = true;        //надо ходить       
                    toolStripStatusLabel4.Text = "Надо ходить";
                    data = Encoding.Unicode.GetBytes("0");   //кодируем сообщение о проигрыше 
                    SendSocket.Send(data);     //отправлем сообщение о проигрыше                 
                    mypole[X + 1, Y + 1] = 3;  //Заменяем ячейку, на стреляную  
                }
                MyPoleGen();     // генерация моего поля        
            }
            else
            {
                // отправка коорд
                data = Encoding.Unicode.GetBytes(X.ToString());  
                SendSocket.Send(data);

                data = Encoding.Unicode.GetBytes(Y.ToString());   
                SendSocket.Send(data);
      
                int bytes = SendSocket.Receive(data, data.Length, 0);  //принять дату 
                if (Encoding.Unicode.GetString(data, 0, bytes) == "1")  //декодировать дату 
                {                  
                    atacking[X + 1, Y + 1] = 2;     //попал задаем режим 2             
                }
                else
                {                   
                    atacking[X + 1, Y + 1] = 3;   //не попал задаем режим 3

                    step = false;  //не ходим
                    timer1.Start();
                }
                MyPoleGen();     //генерация моего поля
                EnemyPoleGen();   //генерация вражьего поля
            }          
        }

        public void Clean()  //очистить матрицу
        {
            for (int z = 0; z <= 11; z++)
            {
                for (int q = 0; q <= 11; q++)
                {
                    mypole[z, q] = 0;
                }
            }

            for (int z = 0; z <= 11; z++)
            {
                for (int q = 0; q <= 11; q++)
                {
                    atacking[z, q] = 0;
                }
            }
        }

        public void Gen()  //герерация кораблей
        {
            double napravl; //равно 0 или 1, отвечает за направление по горизонтали или вертикали
            int x, y;  //координаты первой точки корабля
            int x_end, y_end;
            for (int i = 4; i >= 1; i--)
            {
                for (int j = 0; j < 5 - i; j++)
                {
                    x = rnd.Next(1, 11);
                    y = rnd.Next(1, 11);
                    napravl = rnd.Next(0, 2);
                    if (napravl == 0 && y + i - 1 > 10)
                    {
                        j--;
                        continue;
                    }
                    if (napravl == 1 && x + i - 1 > 10)
                    {
                        j--;
                        continue;
                    }
                    if (napravl == 0)
                    {
                        x_end = x + 1;
                        y_end = y + i;
                    }
                    else
                    {
                        x_end = x + i;
                        y_end = y + 1;
                    }
                    for (int z = x - 1; z <= x_end; z++)
                    {
                        for (int q = y - 1; q <= y_end; q++)
                        {
                            if (mypole[z, q] != 0)
                            {
                                z = 12;
                                y_end = -1;
                                break;
                            }
                        }
                    }
                    if (y_end == -1)
                    {
                        j--;
                        continue;
                    }
                    for (int z = x; z <= x_end - 1; z++)
                    {
                        for (int q = y; q <= y_end - 1; q++)
                        {
                            mypole[z, q] = 1;
                        }
                    }
                }
            }
        }

        //Отрисовываем корабли игрока
        public void MyPoleGen()
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {                 
                    if (mypole[i + 1, j + 1] == 0)    //пустота
                        gr.FillRectangle(new SolidBrush(Color.DodgerBlue), 5 + j * 35 + 2 * j, 75 + i * 35 + 2 * i, 35, 35); //море
                    
                    if (mypole[i + 1, j + 1] == 1)     //корабль
                        gr.FillRectangle(new SolidBrush(Color.BlueViolet), 5 + j * 35 + 2 * j, 75 + i * 35 + 2 * i, 35, 35);  //корабль
                 
                    if (mypole[i + 1, j + 1] == 2)        //попал
                    {
                        gr.FillRectangle(new SolidBrush(Color.DodgerBlue), 5 + j * 35 + 2 * j, 75 + i * 35 + 2 * i, 35, 35);  //море
                        gr.DrawString("X", new System.Drawing.Font("Arial", (float)22), new SolidBrush(Color.Black), 12 + j * 35 + 2 * j, 82 + i * 35 + 2 * i); //крест
                       
                    }
                   
                    if (mypole[i + 1, j + 1] == 3)    //не попал  
                    {
                        gr.FillRectangle(new SolidBrush(Color.DodgerBlue), 5 + j * 35 + 2 * j, 75 + i * 35 + 2 * i, 35, 35); //море
                        gr.FillEllipse(new SolidBrush(Color.Black), 457 + j * 35 + 2 * j, 82 + i * 35 + 2 * i, 20, 20);  //мина у меня на поле для стрельбы
                        gr.FillEllipse(new SolidBrush(Color.Black), 12 + j * 35 + 2 * j, 82 + i * 35 + 2 * i,20,20);    //мина на вражеском поле
                    }
                }
            }
        }

        //Отрисовываем корабли противника (куда стрелял игрок)
        public void EnemyPoleGen()
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {                                  
                    if (atacking[i + 1, j + 1] == 0)      //пустота
                        gr.FillRectangle(new SolidBrush(Color.DodgerBlue), 450 + j * 35 + 2 * j, 75 + i * 35 + 2 * i, 35, 35); //море
                    
                    if (atacking[i + 1, j + 1] == 2)      //попал
                    {
                        balls = balls+1;
                        toolStripStatusLabel2.Text = Convert.ToString(balls);   //счёт игры                   
                        gr.FillRectangle(new SolidBrush(Color.DodgerBlue), 450 + j * 35 + 2 * j, 75 + i * 35 + 2 * i, 35, 35); //море
                        gr.DrawString("X", new System.Drawing.Font("Arial", (float)22), new SolidBrush(Color.Black), 450 + j * 35 + 2 * j, 75 + i * 35 + 2 * i); //крест на вражьем поле
                        gr.DrawString("X", new System.Drawing.Font("Arial", (float)22), new SolidBrush(Color.Black), 12 + j * 35 + 2 * j, 82 + i * 35 + 2 * i);  //крест у меня на поле для стрельбы
                    }
                   
                    if (atacking[i + 1, j + 1] == 3)      //не попал
                    {
                        toolStripStatusLabel2.Text = Convert.ToString(balls);
                        gr.FillRectangle(new SolidBrush(Color.DodgerBlue), 450 + j * 35 + 2 * j, 75 + i * 35 + 2 * i, 35, 35);  //море
                        gr.FillEllipse(new SolidBrush(Color.Black), 457 + j * 35 + 2 * j, 82 + i * 35 + 2 * i, 20, 20);  //мина у меня на поле для стрельбы
                        gr.FillEllipse(new SolidBrush(Color.Black), 12 + j * 35 + 2 * j, 82 + i * 35 + 2 * i,20,20);     //мина на вражеском поле
                    }
                }
            }
        }

        private void Form1_Shown(object sender, EventArgs e) //функция срабатывет при обновлении формы
        {        
            MyPoleGen();          //генерация моего поля
            EnemyPoleGen();         //генерация вражьего поля
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Clean(); //чистка дисплея
            Gen();   //общая генерация
            MyPoleGen();    // генерация моего поля
            EnemyPoleGen();  // генерация вражьего поля

            //массив сгенерированных кораблей, превращается в строку, для отправки другому игроку
            mypolesend = null;
            for (int z = 0; z <= 11; z++)
            {
                for (int q = 0; q <= 11; q++)
                {
                    mypolesend = mypolesend + mypole[z, q];  //формирование строки
                }
            }
            Connect();
        }

        public bool connecting;
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (connecting == true)  //если соединение установлена 
            { 
                if (step == true)  //если хожу 
                { 
                    int x = 1000000; //infinity
                    int y = 1000000; //
                    if (e.X > 450 && e.Y < 475 && e.X < 819 && e.Y > 75)   //самая важная часть - проверяет поле
                    {                                                      //
                        x = ((e.X - 450) / 37);    // превращает координваты в матрицы                        
                        y = (e.Y - 75) / 37;                               //
                       
                        if (radioButton1.Checked == true) //если сервер играет
                            atack(y, x, handler);    //отправить только что нажатые координаты
                        else                        //если клиент играет
                            atack(y, x, listenSocket);   //отправить только что нажатые координаты
                    }
                }
            }
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            atack(0, 0, SockettoTimer);  //запуск функции отправки и анализа координат в таймер
        }
        
    }
}
