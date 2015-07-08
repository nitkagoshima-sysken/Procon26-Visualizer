using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using System.Diagnostics;

namespace presentation
{
    public partial class PresenForm : Form
    {
        public PresenForm()
        {
            InitializeComponent();
        }

        private const int board_size = 32;
        private const int offset = 8;
        private const int stone_size = 8;

        private int[,]       board = new int[(board_size + offset * 2), (board_size + offset * 2)];
        private List<bool[]> stones = new List<bool[]>();
        private List<string> answers = new List<string>();

        private int index = 0;
        private int stoneNum = 0;

        private int hash = 0;

        private void PresenForm_Load(object sender, EventArgs e)
        {
            // コマンドライン引数を配列で取得する
            string[] cmds = Environment.GetCommandLineArgs();

            if (cmds.Length == 3)
            {
                txtProb.Text = cmds[1]; // 問題ファイルを指定
                txtAns.Text  = cmds[2]; // 解答ファイルを指定
            }

            hash = DateTime.Now.Second;

            for (int y = 0; y < board_size + offset * 2; y++)
            {
                for (int x = 0; x < board_size + offset * 2; x++)
                {
                    board[y, x] = -1;
                }
            }
        }

        private void Init()
        {
            // タイマー停止
            timer.Stop();
            index = 0;
            stoneNum = 0;

            // 画面操作ロックを解除
            btnPlay.Enabled = true;
            numSpeed.Enabled = true;
            txtProb.Enabled = true;
            txtAns.Enabled = true;

            // ストップボタンを無効
            btnStop.Enabled = false;
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (txtProb.Text == "" || txtAns.Text == "") 
            {
                MessageBox.Show("Input File Name");
                return;
            }

            // 画面操作をロック
            btnPlay.Enabled = false;
            numSpeed.Enabled = false;
            txtProb.Enabled = false;
            txtAns.Enabled = false;

            // ストップボタンを有効
            btnStop.Enabled = true;

            // 画面更新の頻度を設定
            timer.Interval = (int)(10000 / numSpeed.Value);
        
            stones = new List<bool[]>();
            answers = new List<string>();

            // 問題取得
            getProblem(txtProb.Text);

            // 解答取得
            getAnswer(txtAns.Text);

            // Play
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            // 終了
            if (index == stoneNum)
            {
                Init();

                using (StreamWriter sw = new StreamWriter("result.txt"))
                {
                    int cnt = 0;
                    for (int y = 0; y < board_size; y++)
                    {
                        for (int x = 0; x < board_size; x++)
                        {
                            switch (board[y + offset, x + offset])
                            { 
                                case -1:
                                    sw.Write("■");
                                    break;

                                case 0:
                                    sw.Write("　");
                                    cnt++;
                                    break;

                                default:
                                    sw.Write("＃");
                                    break;
                            }
                        }
                        sw.WriteLine("");
                    }
                    sw.WriteLine("");
                    sw.WriteLine("空白の数は{0}個", cnt);
                }

                return;
            }

            // 石を置く
            Put(index++);

            // デバッグ用
            //for (int i = 0; i < stone_size; i++)
            //{
            //    for (int j = 0; j < stone_size; j++)
            //    {
            //        Debug.Write((stones[i][j + i * stone_size]) ? "□" : "■");
            //    }
            //    Debug.WriteLine("");
            //}
            //Debug.WriteLine("");

            // 進捗表示
            this.Text = "Presentation  " + index.ToString() + " / " + stoneNum.ToString();

            // 画面更新
            pctBoard.Invalidate();
        }

        // 描画
        private void pctBoard_Paint(object sender, PaintEventArgs e)
        {
            
            Graphics g = e.Graphics;

            const int SIZE = 20;

            for (int i = 0; i <= board_size; i++)
            {
                Pen pen = new Pen((i % 5 == 0) ? Brushes.GreenYellow : Brushes.Silver, 1);
                g.DrawLine(pen, new Point(0, SIZE * i), new Point(SIZE * board_size, SIZE * i)); // 横線
                g.DrawLine(pen, new Point(SIZE * i, 0), new Point(SIZE * i, SIZE * board_size)); // 縦線
            }

            if (btnPlay.Enabled) return;
            string result;
            for (int y = 0; y < board_size; y++)
            {
                for (int x = 0; x < board_size; x++)
                {
                    Rectangle rect = new Rectangle(x * SIZE + 1, y * SIZE + 1, SIZE - 1, SIZE - 1);
                    if (board[y + offset, x + offset] == -1)
                    {
                        g.FillRectangle(Brushes.Black, rect);
                    }
                    else if (board[y + offset, x + offset] > 0)
                    {
                        result = calcMd5((board[y + offset, x + offset] + hash).ToString());
                        
                        int R = hexToInt(result[1]) * 16 + hexToInt(result[2]);
                        int G = hexToInt(result[3]) * 16 + hexToInt(result[4]);
                        int B = hexToInt(result[5]) * 16 + hexToInt(result[6]);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(R, G, B)), rect);
                    }
                }
            }

            if (index - 1 >= answers.Count || answers[index - 1] == "")
            {
                toolStripStatusLabel1.Text = string.Format("stone{0} : Pass", index);
                return;
            }

            string[] ans = answers[index - 1].Split(' ');
            Point pos = new Point(int.Parse(ans[0]), int.Parse(ans[1]));

            g.DrawRectangle(Pens.Red, new Rectangle(SIZE * pos.X, SIZE * pos.Y, SIZE * stone_size, SIZE * stone_size));

            toolStripStatusLabel1.Text = string.Format("stone{0} : {1}", index, answers[index - 1]);
        }

        //--------------------------------------------------------------------
        /// <summary>  指定された文字列をMD5でハッシュ化し、文字列として返す
        /// </summary>
        /// <param name="srcStr">入力文字列</param>
        /// <returns>入力文字列のMD5ハッシュ値</returns>
        //--------------------------------------------------------------------
        private string calcMd5(string srcStr)
        {

            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

            // md5ハッシュ値を求める
            byte[] srcBytes = System.Text.Encoding.UTF8.GetBytes(srcStr);
            byte[] destBytes = md5.ComputeHash(srcBytes);

            // 求めたmd5値を文字列に変換する
            System.Text.StringBuilder destStrBuilder;
            destStrBuilder = new System.Text.StringBuilder();
            foreach (byte curByte in destBytes)
            {
                destStrBuilder.Append(curByte.ToString("x2"));
            }

            // 変換後の文字列を返す
            return destStrBuilder.ToString();
        }

        private int hexToInt(char hex)
        {
            switch (hex)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'a': return 10;
                case 'B': return 11;
                case 'b': return 11;
                case 'C': return 12;
                case 'c': return 12;
                case 'D': return 13;
                case 'd': return 13;
                case 'E': return 14;
                case 'e': return 14;
                case 'F': return 15;
                case 'f': return 15;
                default : return 0;
            }
        }

        private bool getProblem(string filename)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    // Boardの読み込み
                    for (int y = 0; y < board_size; y++)
                    {
                        // 一行読み込み
                        string s = sr.ReadLine();
                        
                        for (int x = 0; x < board_size; x++)
                        {
                            if (s[x] == '1')
                            {
                                board[y + offset, x + offset] = -1; // ブロックを置く
                            }
                            else 
                            {
                                board[y + offset, x + offset] =  0; // 空ブロックを置く
                            }
                        }
                    }

                    // 一行飛ばす
                    sr.ReadLine();

                    // 石の数を読み込む
                    stoneNum = int.Parse(sr.ReadLine());

                    // 石を読み込む
                    for (int i = 0; i < stoneNum; i++)
                    {
                        bool[] stn = new bool[stone_size * stone_size];

                        for (int y = 0; y < stone_size; y++)
                        {
                            // 一行読み込み
                            string s = sr.ReadLine();

                            for (int x = 0; x < stone_size; x++)
                            {
                                if (s[x] == '1')
                                {
                                    stn[getIndex(x, y)] = true;  // ブロックを置く
                                }
                                else
                                {
                                    stn[getIndex(x, y)] = false; // 空ブロックを置く
                                }
                            }
                        }

                        // リストに追加する
                        stones.Add(stn);

                        // 一行飛ばす
                        sr.ReadLine(); 
                    }
                }
                return true;
            }
            catch (FileNotFoundException)
            { 
                // ファイルが存在しない
                MessageBox.Show("FileNotFound. (Problem)", "Error");
                return false;
            }
        }

        private bool getAnswer(string filename)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    for (int i = 0; i < stoneNum; i++)
                    { 
                        string s = sr.ReadLine();
                        answers.Add(s);
                    }                    
                }
                return true;
            }
            catch (FileNotFoundException)
            {
                // ファイルが存在しない
                MessageBox.Show("FileNotFound. (Answer)", "Error");
                return false;
            }
        }
        
        private int getIndex(int x, int y)
        {
            return x + y * stone_size;
        }

        // 石の反転
        private void reverce(int i)
        {
            for (int y = 0; y < stone_size; y++)
            {
                for (int x1 = 0, x2 = stone_size - 1; x1 < x2; x1++, x2--)
                {
                    bool tmp = stones[i][getIndex(x1, y)];
                    stones[i][getIndex(x1, y)] = stones[i][getIndex(x2, y)];
                    stones[i][getIndex(x2, y)] = tmp;
                }
            }
        }

        // 石の転置
        private void transposition(int i)
        {
            for (int y = 0; y < stone_size; y++)
            {
                for (int x = y + 1; x < stone_size; x++)
                {
                    bool tmp = stones[i][x + stone_size * y];
                    stones[i][x + stone_size * y] = stones[i][y + stone_size * x];
                    stones[i][y + stone_size * x] = tmp;
                }
            }
        }

        // 石の回転
        private void turn90(int i)
        {
            transposition(i);
            reverce(i);
        }

        private void turn180(int i)
        {
            int n = stone_size * stone_size;
            for (int j = 0; j < n / 2; j++)
            {
                bool tmp = stones[i][j];
                stones[i][j] = stones[i][n - j - 1];
                stones[i][n - j - 1] = tmp;
            }
        }

        private void turn270(int i)
        {
            reverce(i);
            transposition(i);
        }

        private void Put(int i)
        {
            if (answers[i] == "") return;

            string[] ans = answers[i].Split(' ');
            int x = int.Parse(ans[0]);
            int y = int.Parse(ans[1]);

            switch (ans[2])
            { 
                case "H": // 表
                    // 何もしない
                    break;

                case "T": // 裏
                    reverce(i);
                    break;
            }

            // 石の回転
            switch (ans[3])
            { 
                case "90":
                    turn90(i);
                    break;

                case "180":
                    turn180(i);
                    break;

                case "270":
                    turn270(i);
                    break;
                    
            }

            for (int yy = 0; yy < stone_size; yy++)
            {
                for (int xx = 0; xx < stone_size; xx++)
                {
                    int nx = x + xx + offset;
                    int ny = y + yy + offset;

                    if (nx < 0 || nx >= board_size + offset) break;
                    if (ny < 0 || ny >= board_size + offset) break;

                    if (stones[i][getIndex(xx, yy)])
                    {
                        board[ny, nx] = i + 2;
                    }
                }
            }
        }

        // ファイルドラッグでファイル名を入力
        private void txtProb_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void txtProb_DragDrop(object sender, DragEventArgs e)
        {
            //ドロップされたファイルの一覧を取得
            string[] fileName = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (fileName.Length <= 0)
            {
                return;
            }

            // ドロップ先がTextBoxであるかチェック
            TextBox txtTarget = sender as TextBox;
            if (txtTarget == null)
            {
                return;
            }

            //TextBoxの内容をファイル名に変更
            txtProb.Text = fileName[0];
        }

        private void txtAns_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void txtAns_DragDrop(object sender, DragEventArgs e)
        {
            //ドロップされたファイルの一覧を取得
            string[] fileName = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (fileName.Length <= 0)
            {
                return;
            }

            // ドロップ先がTextBoxであるかチェック
            TextBox txtTarget = sender as TextBox;
            if (txtTarget == null)
            {
                return;
            }

            //TextBoxの内容をファイル名に変更
            txtAns.Text = fileName[0];
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (timer.Enabled)
            {
                timer.Stop();
                btnStop.Text = "Start";
            }
            else
            {
                timer.Start();
                btnStop.Text = "Stop";
            }
        }
    }
}
