using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace OpratingSystemTimeSchedule
{

    public partial class Form1 : Form
    {
        private int SliceTime { get; set; } = 10;
        private int DelayTime { get; set; } = 10;
        private int ContextSwitchTime { get; set; } = 10;
        
        public int CurrentTime { get; set; }
        public int HistoryofCurrentTime { get; set; }
        public int NewJobCounter;
        private readonly Queue _processQueue = new Queue();
        private readonly Queue _doneJob = new Queue();
        private readonly List<DoneJobLog> _joblog = new List<DoneJobLog>();
        public int Curstarttext { get; set; }

        public Form1( )
        {
            InitializeComponent();            
        }
       

        private void ReadJobFromFile() 
        {
            using (var reader = new StreamReader(File.OpenRead("Jobs.csv")))
            {
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        var values = line.Split(',');
                        Process p1 = new Process(values[0], Convert.ToInt32(values[1]), SliceTime);
                        _processQueue.Enqueue(p1);
                        listBox4.Items.Add($"processname = {p1.Name} Runningtime= {p1.Runningtime} RemainTime={p1.Remaintime}");
                        listBox4.SelectedIndex = listBox4.Items.Count - 1;
                    }
                }

            }
        }

        private void ResetAllObjects()
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();
            listBox3.Items.Clear();
            listBox4.Items.Clear();
            listBox5.Items.Clear();
            _doneJob.Clear();
            _joblog.Clear();
            CurrentTime = 0;
            HistoryofCurrentTime = 0;
            groupBox3.Visible = false;
            textAvgWaitingTime.Text = "";
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Timer t1 = new Timer {Interval = 1000};
            t1.Tick += timer1_Tick;
            t1.Enabled = true;
            t1.Start();
            ResetAllObjects();

            SliceTime = Convert.ToInt32(textBox1.Text);
            ContextSwitchTime = Convert.ToInt32(textBox2.Text);
            DelayTime = Convert.ToInt32(textBox3.Text);
            
            ReadJobFromFile();
            
             
            while (true)
            {
                System.Threading.Thread.Sleep(DelayTime);
                if (_processQueue.Count == 0)
                {
                    break;
                }
                
                var p=(Process) _processQueue.Dequeue();
                DoneJobLog tmp = new DoneJobLog();
                tmp.Start = CurrentTime;
                tmp.Processname = p.Name;
                var currentTime = CurrentTime;
                p.DoJob(ref currentTime);
                CurrentTime = currentTime;
                tmp.End = currentTime;
                listBox2.Items.Add(tmp.Processname);
                listBox2.SelectedIndex = listBox2.Items.Count - 1;
                _joblog.Add(tmp);

                if (p.Remaintime > SliceTime)
                {
                    _processQueue.Enqueue(p);
                }
                else
                {
                    _doneJob.Enqueue(p);
                    listBox1.Items.Add(p.Name);
                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                    listBox5.Items.Add(p.GetWaitTime().ToString());
                }
                Refresh();
                CurrentTime+= ContextSwitchTime;

                listBox3.Items.Clear();
                for (int i = 0; i < _processQueue.Count  ; i++)
                {
                    Process a = (Process)_processQueue.Dequeue();
                    listBox3.Items.Add($"processname = {a.Name} Runningtime= {a.Runningtime} RemainTime={a.Remaintime}");
                    _processQueue.Enqueue(a);
                }
            }
            t1.Enabled = false;
            t1.Stop();
            HistoryofCurrentTime = CurrentTime;
            groupBox3.Visible = true;
            ComputeAvrageWaitingTime();
        }

        private void ComputeAvrageWaitingTime()
        {
            long sum=0;
            for (int i = 0; i < _doneJob.Count; i++)
            {
                Process a = (Process)_doneJob.Dequeue();
                sum += a.GetWaitTime();
                _doneJob.Enqueue(a);
            }
            textAvgWaitingTime.Text = ((decimal)sum / _doneJob.Count).ToString(CultureInfo.InvariantCulture);
        }
        private void DrawRotatedTextAt(Graphics gr, float angle,
            string txt, int x, int y, Font theFont, Brush theBrush)
        {
            GraphicsState state = gr.Save();
            gr.ResetTransform();
            gr.RotateTransform(angle);
            gr.TranslateTransform(x, y, MatrixOrder.Append);
            gr.DrawString(txt, theFont, theBrush, 0, 0);
            gr.Restore(state);
        }


        public void PaintLogs()
        { 
        var graphicsObj = CreateGraphics();
            Pen myPen = new Pen(Color.Black, 3);
            Pen clearPen = new Pen(BackColor, 3);
            int startx1 = 100;
            int starty1 = 200;
            int startx2 = 1100;
            int starty2 = 200;

            int startpoint = -15;
            
            graphicsObj.DrawLine(myPen, startx1, starty1, startx2, starty2);
            var myFont = new Font("Tahoma", 8);

            Brush myBrush = new SolidBrush(Color.Red);
             
            int starttext =  (CurrentTime / 100) * 100 ;
            
            int endtext = ((CurrentTime / 100)+ 1)  * 100;

            if (Curstarttext != starttext)
            {
                graphicsObj.DrawLine(clearPen, startx1, starty1+15, startx2, starty2+15);
            }

            Curstarttext = starttext;


            Pen pen1 = new Pen(Color.Blue, 2);
            Pen pen2 = new Pen(Color.DeepPink, 2);
            

            DrawRotatedTextAt(graphicsObj, -90, starttext.ToString(), startx1, starty1 + startpoint, myFont, myBrush);
            DrawRotatedTextAt(graphicsObj, -90, endtext.ToString(), startx2, starty2 + startpoint, myFont, myBrush);

            int counter = 0;
            if (_joblog != null)
                foreach (var a in _joblog)
                {
                    if ((a.Start >= starttext && a.Start <= endtext) || (a.End >= starttext && a.End <= endtext))
                    {
                        var fStart = a.Start >= starttext ? a.Start : starttext;
                        var fEnd = a.End <= endtext ? a.End : endtext;
                        counter++;
                        Brush aBrush = new SolidBrush(counter % 2 == 0 ? pen1.Color : pen2.Color);
                        var x1 = ((startx2 - startx1) / 100) * (fStart % 100) + startx1;
                        DrawRotatedTextAt(graphicsObj, -90, fStart.ToString(), x1, starty1 + startpoint, myFont,
                            aBrush);
                        var x2 = ((startx2 - startx1) / 100) * (fEnd % 100) + startx1;
                        if (x2 < x1) x2 = startx2;
                        DrawRotatedTextAt(graphicsObj, -90, fEnd.ToString(), x2, starty1 + startpoint, myFont, aBrush);
                        graphicsObj.DrawLine(counter % 2 == 0 ? pen1 : pen2, x1, starty1 + 5, x1, starty1 - 5);
                        graphicsObj.DrawLine(counter % 2 == 0 ? pen1 : pen2, x2, starty1 + 5, x2, starty1 - 5);

                        graphicsObj.DrawLine(counter % 2 == 0 ? pen1 : pen2, x1, starty1 - startpoint, x2,
                            starty1 - startpoint);
                        DrawRotatedTextAt(graphicsObj, +90, a.Processname,
                            x1 + (((startx2 - startx1) / 100) * ((fEnd - fStart) / 2)), starty1 + 20, myFont, aBrush);
                    }

                }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!checkBox1.Checked) return;
            Random rnd = new Random();
            int jobduration = rnd.Next(1, 50);
            NewJobCounter++;
            Process p1 = new Process($"AP{NewJobCounter}", jobduration, SliceTime);
            _processQueue.Enqueue(p1);
            listBox4.Items.Add($"processname = {p1.Name} Runningtime= {p1.Runningtime} RemainTime={p1.Remaintime}");
            listBox4.SelectedIndex = listBox4.Items.Count - 1;
        }


        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            PaintLogs();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (CurrentTime < 100) return;
            CurrentTime -= 100;
            Refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (CurrentTime + 100 > HistoryofCurrentTime) return;
            CurrentTime += 100;
            Refresh();
        }
    }
    public class DoneJobLog
    {
        public int Start;
        public int End;
        public string Processname;
    }
    public class ProcessWork
    {
        public int Start;
        public int End;
    }

    public class Process
    {

        public Process(string name, int runningtime, int slicetime)
        {
            Runningtime = runningtime;
            Slicetime = slicetime;
            Remaintime = runningtime;
            Name = name;
            ProcessWork = new List<ProcessWork>();
        }

        public void DoJob(ref int currentTime)
        {
            ProcessWork tmp = new ProcessWork {Start = currentTime};
            currentTime += Remaintime - Slicetime >= 0 ? Slicetime : Remaintime;
            tmp.End = currentTime;
            ProcessWork.Add(tmp);
            Remaintime = Remaintime - Slicetime>=0? Remaintime - Slicetime : Slicetime - Remaintime;
        }

        public int GetWaitTime()
        {
            int previusRun = 0;
            int result = 0;
            foreach (var p in ProcessWork)
            {
                result += p.Start - previusRun;
                previusRun = p.End;
            }
            return result;
        }
        public string Name { get; set; }
        public int Runningtime { get; set; }
        public int Remaintime { get; set; }
        public int Slicetime { get; }
        public List<ProcessWork> ProcessWork { get; set; }
    }
}
