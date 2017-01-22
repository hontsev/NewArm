using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices; 
using Microsoft.Win32; 

namespace NewArm
{
    public partial class Form1 : Form
    {
        private bool getMousePositionRun = true;
        private bool run = false;
        delegate void sendStringDelegate(string str);
        delegate void sendVoidDelegate();
        int num = 0;
        KeyboardHook k_hook;

        string filename = "tasks.json";

        private List<Task> tasks;
        private Task nowTask;

        public Form1()
        {
            InitializeComponent();
            this.Focus();
            comboBox1.SelectedIndex = 0;
            loadTasks();
            updateTasksView();

            initHook();

            //开启捕捉鼠标位置的常驻线程
            new Thread(workGetMousePosition).Start();

            
        }

        private void loadTasks()
        {
            try
            {
                tasks = IOController.getDataFromJson(filename);
            }
            catch
            {
                tasks = new List<Task>();
            }
            
        }

        private void saveTasks()
        {
            try
            {
                IOController.saveDataAsJson(filename, tasks);
            }
            catch
            {

            }
            
        }

        private void updateTasks()
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                this.tasks[i].isRun = checkedListBox1.GetItemChecked(i);
            }
        }

        private void updateTasksView()
        {
            if (checkedListBox1.InvokeRequired)
            {
                sendVoidDelegate mevent = new sendVoidDelegate(updateTasksView);
                Invoke(mevent);
            }
            else
            {
                checkedListBox1.Items.Clear();
                foreach (var i in tasks)
                {
                    string name = i.name;
                    bool isRun = i.isRun;
                    checkedListBox1.Items.Add(name, isRun);
                }
            }
        }

        private void updateTaskItemView()
        {
            if (checkedListBox1.InvokeRequired)
            {
                sendVoidDelegate mevent = new sendVoidDelegate(updateTaskItemView);
                Invoke(mevent);
            }
            else
            {
                if (nowTask == null) return;
                textBox2.Text = nowTask.name;
                comboBox2.SelectedIndex = key2int(nowTask.hotKey);
                listView1.Items.Clear();
                foreach (var i in nowTask.items)
                {
                    ListViewItem item = new ListViewItem(i.command.ToString());
                    item.SubItems.Add(i.args);
                    listView1.Items.Add(item);
                }
            }
        }

        private void addTask()
        {
            tasks.Add(new Task());
            updateTasksView();
            checkedListBox1.SelectedIndex = checkedListBox1.SelectedItems.Count - 1;
            updateTaskItemView();
        }

        private void deleteTask(int index)
        {
            tasks.RemoveAt(index);
            updateTasksView();
            if (tasks.Count > 0) checkedListBox1.SelectedIndex = index ;
            updateTaskItemView();
        }

        private void addTaskItem(Command comm, string args)
        {
            TaskItem item = new TaskItem();
            item.command = comm;
            item.args = args;
            nowTask.items.Add(item);
            updateTaskItemView();
        }

        private void changeTaskItemPosition(int ds)
        {
            int beforep = listView1.SelectedIndices[0];
            int afterp = beforep + ds;
            if (afterp < 0 || afterp >= nowTask.items.Count) return;
            TaskItem i1 = nowTask.items[beforep];
            TaskItem i2 = nowTask.items[afterp];
            Command tmpc = i2.command;
            string tmpa = i2.args;
            i2.args = i1.args;
            i2.command = i1.command;
            i1.args = tmpa;
            i1.command = tmpc;

            updateTaskItemView();
        }

        private void deleteTaskItem(int index)
        {
            nowTask.items.RemoveAt(index);

            updateTaskItemView();
        }

        private void updateTaskItem()
        {
            nowTask.name = textBox2.Text;
            nowTask.hotKey = str2key(comboBox2.SelectedItem.ToString());
        }

        private static Keys str2key(string str)
        {
            if (str == "left") return Keys.Left;
            else if (str == "right") return Keys.Right;
            else if (str == "down") return Keys.Down;
            else if (str == "up") return Keys.Up;
            else if (str == "0") return Keys.D0;
            else if (str == "1") return Keys.D1;
            else if (str == "2") return Keys.D2;
            else if (str == "3") return Keys.D3;
            else if (str == "4") return Keys.D4;
            else if (str == "5") return Keys.D5;
            else if (str == "6") return Keys.D6;
            else if (str == "7") return Keys.D7;
            else if (str == "8") return Keys.D8;
            else if (str == "9") return Keys.D9;
            else return Keys.Space;
        }

        private static int key2int(Keys key)
        {
            if (key == Keys.Left) return 0;
            else if (key == Keys.Right) return 1;
            else if (key == Keys.Up) return 2;
            else if (key == Keys.Down) return 3;
            else if (key == Keys.D0) return 4;
            else if (key == Keys.D1) return 5;
            else if (key == Keys.D2) return 6;
            else if (key == Keys.D3) return 7;
            else if (key == Keys.D4) return 8;
            else if (key == Keys.D5) return 9;
            else if (key == Keys.D6) return 10;
            else if (key == Keys.D7) return 11;
            else if (key == Keys.D8) return 12;
            else if (key == Keys.D9) return 13;
            else return 0;
        }





        /// <summary>
        /// 安装键盘钩子
        /// </summary>
        private void initHook()
        {
            k_hook = new KeyboardHook();
            k_hook.KeyDownEvent += new KeyEventHandler(hook_KeyDown);//钩住键按下 
            k_hook.Start();//安装键盘钩子
        }

        /// <summary>
        /// 取消钩子
        /// </summary>
        private void stopHook()
        {
            try
            {
                k_hook.Stop();
            }
            catch 
            { 

            }
        }

        private void dealTaskItem(TaskItem item)
        {
            switch (item.command)
            {
                case Command.mouseMovStatic:
                    int x = 0;
                    int y = 0;
                    int.TryParse(item.args.Split(' ')[0], out x);
                    int.TryParse(item.args.Split(' ')[1], out y);
                    mouseMoveTo(x, y);
                    break;
                case Command.mouseMov:
                    int dx = 0;
                    int dy = 0;
                    int.TryParse(item.args.Split(' ')[0], out dx);
                    int.TryParse(item.args.Split(' ')[1], out dy);
                    mouseMoveToD(dx, dy);
                    break;
                case Command.mouseLC:
                    mouseLeftClick();
                    break;
                case Command.mouseLD:
                    mouseLeftDown();
                    break;
                case Command.mouseLU:
                    mouseLeftUp();
                    break;
                case Command.wait:
                    int time = 0;
                    int.TryParse(item.args, out time);
                    Thread.Sleep(time);
                    break;
                default:
                    break;
            }
        }

        private void dealTask(Keys key)
        {
            foreach (var task in tasks)
            {
                if (task.isRun && task.hotKey == key)
                {
                    int beginx = MousePosition.X;
                    int beginy = MousePosition.Y;

                    foreach (var item in task.items)
                    {
                        
                        dealTaskItem(item);
                    }

                    mouseMoveTo(beginx, beginy);
                }
            }
        }

        private void hook_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.KeyValue == (int)Keys.A && (int)Control.ModifierKeys == (int)Keys.Alt)
            if (run)
            {
                dealTask(e.KeyCode);
            }


            if (e.KeyCode == Keys.A)
            {
                if (leftloop) stopLeftMouseClickLoop();
                else startLeftMouseClickLoop();
            }

            //if (e.KeyCode == Keys.Left)
            //{
            //    new Thread(zhihuJubao).Start();
            //}
            //else if (e.KeyCode == Keys.Right)
            //{
            //    new Thread(zhihuJubao2).Start();
            //}
            //else if (e.KeyCode == Keys.Down)
            //{
            //    new Thread(zhihuJubao3).Start();
            //}
        }

        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        //移动鼠标 
        const int MOUSEEVENTF_MOVE = 0x0001;
        //模拟鼠标左键按下 
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        //模拟鼠标左键抬起 
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        //模拟鼠标右键按下 
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        //模拟鼠标右键抬起 
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //模拟鼠标中键按下 
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        //模拟鼠标中键抬起 
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        //标示是否采用绝对坐标 
        const int MOUSEEVENTF_ABSOLUTE = 0x8000; 
        
        public void print(string str)
        {
            if (textBox1.InvokeRequired)
            {
                sendStringDelegate mevent = new sendStringDelegate(print);
                Invoke(mevent, (object)str);
            }
            else
            {
                textBox1.AppendText(str + "\r\n");
            }
        }

        public void printLabel(string str)
        {
            if (label5.InvokeRequired)
            {
                sendStringDelegate mevent = new sendStringDelegate(printLabel);
                Invoke(mevent, (object)str);
            }
            else
            {
                label5.Text = str;
            }
        }

        public void workGetMousePosition()
        {
            while (getMousePositionRun)
            {
                printLabel(string.Format("x:{0},y:{1}", MousePosition.X, MousePosition.Y));
                Thread.Sleep(500);
            }
        }

        public void leftClick()
        {
            while (run)
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(1000);
                mouse_event(MOUSEEVENTF_MOVE, 10, 10, 0, 0);
                Thread.Sleep(1000);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(1000);
                mouse_event(MOUSEEVENTF_MOVE, -10, -10, 0, 0);
                Thread.Sleep(1000);
            }
        }

        private void printScreen()
        {
            
            //System.Threading.Thread.Sleep(200);
            Bitmap bit = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(bit);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), bit.Size);

            bit.Save("images/" + num + ".jpg");
            num++;

            g.Dispose();
            //this.Visible = true;
        }

        private void getImage()
        {
            num = 1;
            Thread.Sleep(3000);
            //this.Visible = false;
            while (run)
            {
                Thread.Sleep(2000);
                printScreen();
                Thread.Sleep(300);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(300);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }
        }

        private void cutTwoImage()
        {
            Bitmap limg = new Bitmap(570, 708);
            Bitmap rimg = new Bitmap(570, 708);
            Image srcimg = Image.FromFile("images/" + (num / 2 + 1) + ".jpg");
            

            Graphics g = Graphics.FromImage(limg);
            g.DrawImage(
                srcimg, 
                new Rectangle(0, 0, limg.Width, limg.Height), 
                new Rectangle(113, 18, limg.Width, limg.Height), 
                GraphicsUnit.Pixel);
            limg.Save("images2/" + num + ".jpg");
            g.Dispose();
            num++;
            g = Graphics.FromImage(rimg);
            g.DrawImage(
                srcimg,
                new Rectangle(0, 0, limg.Width, limg.Height),
                new Rectangle(683, 18, limg.Width, limg.Height),
                GraphicsUnit.Pixel);
            rimg.Save("images2/" + num + ".jpg");
            g.Dispose();
            num++;
        }

        private void cutImage()
        {
            num = 0;
            //this.Visible = false;
            while (run)
            {
                //Thread.Sleep(2000);
                cutTwoImage();
            }
        }

        double tx = 48.1927710843;
        double ty = 85.4700854701;

        private void mouseMoveTo(int x, int y)
        {
            mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, (int)(x * tx), (int)(y * ty), 0, 0);
            Thread.Sleep(50);
        }

        private void mouseMoveToD(int dx, int dy)
        {
            mouse_event(MOUSEEVENTF_MOVE, (int)(1* dx), (int)(1* dy), 0, 0);
            Thread.Sleep(50);
        }

        private void mouseLeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        private void mouseClickAt(int x, int y)
        {
            mouseMoveTo(x, y);
            mouseLeftClick();
        }

        private void mouseClickAtD(int dx, int dy)
        {
            mouseMoveToD(dx, dy);
            mouseLeftClick();
        }

        private void mouseLeftDown()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        }

        private void mouseLeftUp()
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// 新版举报回答
        /// </summary>
        private void zhihuJubao3()
        {
            if (run)
            {
                int beginx = MousePosition.X;
                int beginy = MousePosition.Y;
                mouseLeftClick();
                Thread.Sleep(200);
                //mouseClickAtD(0, -30);
                //Thread.Sleep(300);
                mouseClickAt(657, 387);
                Thread.Sleep(100);
                mouseClickAt(677, 414);
                Thread.Sleep(100);
                mouseMoveTo(920, 516);
                mouseLeftDown();
                mouseMoveTo(920, 670);
                mouseLeftUp();
                mouseClickAt(778, 631);

                mouseMoveTo(beginx, beginy);

            }
        }

        private void zhihuJubao2()
        {
            if (run)
            {
                int beginx = MousePosition.X;
                int beginy = MousePosition.Y;
                mouseLeftClick();
                Thread.Sleep(200);
                mouseClickAtD(0, -30);
                Thread.Sleep(300);
                mouseClickAt(657, 387);
                Thread.Sleep(100);
                mouseClickAt(677, 414);
                Thread.Sleep(100);
                mouseMoveTo(920, 516);
                mouseLeftDown();
                mouseMoveTo(920, 670);
                mouseLeftUp();
                mouseClickAt(778, 631);

                mouseMoveTo(beginx, beginy);
                //while (true)
                //{
                //    print(string.Format("x:{0},y:{1}", MousePosition.X, MousePosition.Y));
                //}
            }
        }


        private void zhihuJubao()
        {
            if (run)
            {
                //print("开始");
                int beginx = MousePosition.X;
                int beginy = MousePosition.Y;
                mouseLeftClick();
                mouseClickAt(665, 314);
                mouseClickAt(665, 380);
                mouseClickAt(890, 514);
                Thread.Sleep(200);
                mouseClickAt(921, 203);
                mouseMoveTo(beginx, beginy);
            }
        }

        private bool leftloop = false;

        private void workLeftMouseClickLoop()
        {
            while (leftloop)
            {
                mouseLeftDown();
                Thread.Sleep(50);
                mouseLeftUp();
                Thread.Sleep(50);
            }
        }

        private void startLeftMouseClickLoop()
        {
            leftloop = true;
            new Thread(workLeftMouseClickLoop).Start();
        }

        private void stopLeftMouseClickLoop()
        {
            leftloop = false;
        }

































        private void button1_Click(object sender, EventArgs e)
        {
            if (run)
            {
                run = false;
                print("已中止热键响应.");
                button1.Text = "激活";
            }
            else
            {
                run = true;
                print("开始响应热键.");
                button1.Text = "停止";
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                stopHook();
                getMousePositionRun = false;
                saveTasks();
            }
            catch
            {

            }

            Environment.Exit(0);
        }

        private void checkedListBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(MousePosition);
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (checkedListBox1.SelectedItem != null)
                {
                    int index = checkedListBox1.SelectedIndex;
                    nowTask = tasks[index];
                    updateTaskItemView();
                }
                updateTasks();
            }
        }

        private void 新建任务ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addTask();
        }

        private void 删除任务ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.SelectedItems.Count > 0)
            {
                deleteTask(checkedListBox1.SelectedIndex);
            }
            
        }

        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip2.Show(MousePosition);
            }
        }

        private void 上移ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count>0)
                changeTaskItemPosition(-1);
        }

        private void 下移ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
                changeTaskItemPosition(1);
        }

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                deleteTaskItem(listView1.SelectedIndices[0]);
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Command comm=(Command)comboBox1.SelectedIndex;
            if (comm == Command.mouseMov || comm == Command.mouseMovStatic)
                addTaskItem(comm, string.Format("{0} {1}", textBox3.Text, textBox4.Text));
            else
                addTaskItem(comm, string.Empty);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            addTaskItem(Command.key, textBox5.Text);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            addTaskItem(Command.wait, numericUpDown1.Value.ToString());
        }

        private void button5_Click(object sender, EventArgs e)
        {
            updateTaskItem();
            updateTasksView();
        }

        

        private void button6_Click(object sender, EventArgs e)
        {
            if (leftloop) stopLeftMouseClickLoop();
            else startLeftMouseClickLoop();
        }



    }
}
