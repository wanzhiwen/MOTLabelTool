using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Drawing.Imaging;

namespace MOTLabelTool
{
    public partial class FormMain : Form
    {             
        string imagePath_ = "";
        string annoPath_ = "";
        List<string> imageNames = new List<string>();
        Dictionary<int, Dictionary<int, TrackObject>> anno = new Dictionary<int, Dictionary<int, TrackObject>>();
        int selectedObjectIndex = -1;
        int selectedFrameIndex = -1;
        int frameNum = -1;
        int zoom_ = 1;// scale
        bool isreset_ = false;
        int prelistBoxFileIndex_ = -1;

        // Constructor
        public FormMain()
        {
            InitializeComponent();
        }

        // Main window loading
        private void FormMain_Load(object sender, EventArgs e)
        {
            LoadPics();// Load picture
            LoadAnno();// Load annotation
        }

        // Load all images in the image path
        void LoadPics()
        {
            if(imagePath_ == "") return;
            if(imagePath_ != "" && !Directory.Exists(imagePath_))
            {
                Directory.CreateDirectory(imagePath_);
                return;
            }
            imageNames.Clear();
            var dir = new DirectoryInfo(imagePath_);
            int count = 0;
            foreach (var file in dir.GetFiles())
            {
                if (file.Extension == ".jpg")
                {
                    imageNames.Add(file.FullName);
                    count++;
                }
            }
            frameNum = count;
        }

        // 从标注文件中导入程序
        private void LoadAnno()
        {
            if(annoPath_ == "") return;
            if(annoPath_ != "" && !File.Exists(annoPath_))
            {
                File.Create(annoPath_);
                return;
            }
            System.IO.StreamReader file = new System.IO.StreamReader(annoPath_);
            string aLine;
            while(true)
            {
                aLine = file.ReadLine();
                if(aLine == null) break;
                var items = aLine.Split(',');
                var frame_id = Convert.ToInt32(items[0]);
                var object_id = Convert.ToInt32(items[1]);
                var x = Convert.ToInt32(items[2]);
                var y = Convert.ToInt32(items[3]);
                var w = Convert.ToInt32(items[4]);
                var h = Convert.ToInt32(items[5]);
                var object_type = Convert.ToInt32(items[6]);
                var cloth_num = Convert.ToInt32(items[7]);
                var shot_type = Convert.ToInt32(items[8]);
                var is_soc = Convert.ToInt32(items[9]);
                var is_doc = Convert.ToInt32(items[10]);
                var is_moc = Convert.ToInt32(items[11]);
                var is_foc = Convert.ToInt32(items[12]);
                var is_mb = Convert.ToInt32(items[13]);
                var is_ov = Convert.ToInt32(items[14]);
                TrackObject track_object = new TrackObject(frame_id, object_id, x, y, w, h, object_type, cloth_num, shot_type, is_soc, is_doc, is_moc, is_foc, is_mb, is_ov);
                if(!anno.ContainsKey(frame_id))
                {
                    Dictionary<int, TrackObject> d = new Dictionary<int, TrackObject>();
                    anno[frame_id] = d;
                }
                anno[frame_id][object_id] = track_object;
            }
            file.Close();
        }

        private Bitmap ZoomImage(Bitmap bitmap, int destHeight, int destWidth)
        {
            try
            {
                System.Drawing.Image sourImage = bitmap;
                int width = 0, height = 0;
                //Scaling             
                int sourWidth = sourImage.Width;
                int sourHeight = sourImage.Height;
                if (sourHeight > destHeight || sourWidth > destWidth)
                {
                    if ((sourWidth * destHeight) > (sourHeight * destWidth))
                    {
                        width = destWidth;
                        height = (destWidth * sourHeight) / sourWidth;
                    }
                    else
                    {
                        height = destHeight;
                        width = (sourWidth * destHeight) / sourHeight;
                    }
                }
                else
                {
                    width = sourWidth;
                    height = sourHeight;
                }
                Bitmap destBitmap = new Bitmap(destWidth, destHeight);
                Graphics g = Graphics.FromImage(destBitmap);
                g.Clear(Color.Transparent);
                //Set the drawing quality of the canvas           
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                g.DrawImage(sourImage, new Rectangle((destWidth - width) / 2, (destHeight - height) / 2, width, height), 0, 0, sourImage.Width, sourImage.Height, GraphicsUnit.Pixel);
                g.Dispose();
                //Set compression quality     
                System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters();
                long[] quality = new long[1];
                quality[0] = 100;
                System.Drawing.Imaging.EncoderParameter encoderParam = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                encoderParams.Param[0] = encoderParam;
                sourImage.Dispose();
                return destBitmap;
            }
            catch
            {
                return bitmap;
            }
        }

        // Triggered when  selects change
        private void SelectedIndexChanged(object sender, EventArgs e)
        {


            if (isreset_)
            {
                zoom_ = 1;
                prelistBoxFileIndex_ = selectedFrameIndex;
            }
            
            isreset_ = false;

            if (selectedFrameIndex != -1 && selectedObjectIndex != -1)
            {
                var jpgPath = imageNames[selectedFrameIndex];
                pictureBox1.ImageLocation = jpgPath;
                pictureBox1.Load(jpgPath);

                pictureBox1.Width = pictureBox1.Image.Width * zoom_;
                pictureBox1.Height = pictureBox1.Image.Height * zoom_;

                ClearSelect();  // Delete the previously selected redraw
                listBoxLable_MouseClick(null, null);//画图

                if(!anno.ContainsKey(selectedFrameIndex)) anno[selectedFrameIndex] = new Dictionary<int, TrackObject>();
                if(!anno[selectedFrameIndex].ContainsKey(selectedObjectIndex)) anno[selectedFrameIndex][selectedObjectIndex] = new TrackObject(selectedFrameIndex, selectedObjectIndex, 0,0,0,0,0,0,0,0,0,0,0,0,0);

                var trackObject = anno[selectedFrameIndex][selectedObjectIndex];
                this.xTextBox.Text = Convert.ToString(trackObject.bbox_x);
                this.yTextBox.Text = Convert.ToString(trackObject.bbox_y);
                this.widthTextBox.Text = Convert.ToString(trackObject.bbox_w);
                this.heightTextBox.Text = Convert.ToString(trackObject.bbox_h);
                this.frameIdTextBox.Text = Convert.ToString(trackObject.frame_id);
                this.ObjectIdTextBox.Text = Convert.ToString(trackObject.object_id);
                this.objectTypeTextBox.Text = Convert.ToString(trackObject.object_type);
                this.clothNumTextBox.Text = Convert.ToString(trackObject.cloth_num);
                this.shotTypeTextBox.Text = Convert.ToString(trackObject.shot_type);
                if(trackObject.is_soc > 0) this.checkBox1.CheckState = CheckState.Checked;
                else this.checkBox1.CheckState = CheckState.Unchecked;
                if(trackObject.is_doc > 0) this.checkBox2.CheckState = CheckState.Checked;
                else this.checkBox2.CheckState = CheckState.Unchecked;
                if(trackObject.is_moc > 0) this.checkBox3.CheckState = CheckState.Checked;
                else this.checkBox3.CheckState = CheckState.Unchecked;
                if(trackObject.is_foc > 0) this.checkBox4.CheckState = CheckState.Checked;
                else this.checkBox4.CheckState = CheckState.Unchecked;
                if(trackObject.is_mb > 0) this.checkBox5.CheckState = CheckState.Checked;
                else this.checkBox5.CheckState = CheckState.Unchecked;
                if(trackObject.is_ov > 0) this.checkBox6.CheckState = CheckState.Checked;
                else this.checkBox6.CheckState = CheckState.Unchecked;
            }
        }

        private Point RectStartPoint;
        private Rectangle Rect = new Rectangle();


        private void pictureBox1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Picture frame
                RectStartPoint = e.Location;
                Invalidate();
        }

        private void pictureBox1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {


            if (e.Button != MouseButtons.Left)//Determine whether press the left button
                return;
                    
            Point tempEndPoint = e.Location; //Record the position and size of the box
            Rect.Location = new Point(Math.Min(RectStartPoint.X, tempEndPoint.X),Math.Min(RectStartPoint.Y, tempEndPoint.Y));
            Rect.Size = new Size(Math.Abs(RectStartPoint.X - tempEndPoint.X),Math.Abs(RectStartPoint.Y - tempEndPoint.Y));

            pictureBox1.Invalidate();
            
        }

        private void pictureBox1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                
                if (Rect != null && Rect.Width > 0 && Rect.Height > 0)
                {
                    e.Graphics.DrawRectangle(new Pen(Color.Red, 3), Rect);//Repaint the color to red  
                }              
            }
        }


        void ClearSelect()
        {
            Rect.Size = new Size(0, 0);
            pictureBox1.Refresh();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            
                if (e.Button == MouseButtons.Left)
                {
                    if (RectStartPoint == e.Location)
                    {
                        return;
                    }

                    if (RectStartPoint.X > e.Location.X || RectStartPoint.Y > e.Location.Y)
                    {
                        MessageBox.Show("Only select from top left to bottom right");
                        ClearSelect();
                        scaleRect();
                        return;
                    }
                    anno[selectedFrameIndex][selectedObjectIndex].bbox_x = RectStartPoint.X / zoom_;
                    anno[selectedFrameIndex][selectedObjectIndex].bbox_y = RectStartPoint.Y / zoom_;
                    anno[selectedFrameIndex][selectedObjectIndex].bbox_w = (e.Location.X - RectStartPoint.X) / zoom_;
                    anno[selectedFrameIndex][selectedObjectIndex].bbox_h = (e.Location.Y - RectStartPoint.Y) / zoom_;
                    ClearSelect();  // Delete the previously selected redraw
                    SelectedIndexChanged(null, null);
                }
            
        }

        private void buttonImport_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dilog = new FolderBrowserDialog();
            dilog.Description = "Please select a folder";
            if (dilog.ShowDialog() == DialogResult.OK || dilog.ShowDialog() == DialogResult.Yes)
            {
                imagePath_ = dilog.SelectedPath + "\\";
                annoPath_ = dilog.SelectedPath + "\\anno.txt";
                FormMain_Load(null, null);
            }
            this.tFrameLabel.Text = "Total Frame: " + Convert.ToString(frameNum);
            selectedFrameIndex = 0;
            selectedObjectIndex = 0;
            SelectedIndexChanged(null, null);

        }

        //这是画图函数，后续改个名字todo 连用两个if不好
        private void listBoxLable_MouseClick(object sender, MouseEventArgs e)
        {
            if (selectedFrameIndex != -1 && selectedObjectIndex != -1 && anno.ContainsKey(selectedFrameIndex) && anno[selectedFrameIndex].ContainsKey(selectedObjectIndex))
            {
                TrackObject trackObject = anno[selectedFrameIndex][selectedObjectIndex];
                Rect.Location = new Point(trackObject.bbox_x * zoom_, trackObject.bbox_y * zoom_);
                Rect.Size = new Size(trackObject.bbox_w * zoom_, trackObject.bbox_h * zoom_);
                pictureBox1.Invalidate();
            }
        }


        // Handle some keyboard operations
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (selectedObjectIndex == -1 || selectedFrameIndex == -1)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (keyData != Keys.NumPad8 && keyData != Keys.NumPad5 && keyData != Keys.NumPad4 && keyData != Keys.NumPad6 && keyData != Keys.I && keyData != Keys.J && keyData != Keys.K && keyData != Keys.L
                && keyData != Keys.W && keyData != Keys.A && keyData != Keys.S && keyData != Keys.D && keyData != Keys.Up && keyData != Keys.Down && keyData != Keys.Left && keyData != Keys.Right)
            {
                return base.ProcessCmdKey(ref msg, keyData); ;
            }

            if (selectedFrameIndex != -1 && selectedObjectIndex != -1)
            {
                var trackObject = anno[selectedFrameIndex][selectedObjectIndex];
                if (trackObject.bbox_w != 0 && trackObject.bbox_h != 0)
                {
                    int x = trackObject.bbox_x;
                    int y = trackObject.bbox_y;
                    int w = trackObject.bbox_w;
                    int h = trackObject.bbox_h;

                    int moveSteps = 1;

                    switch (keyData)
                    {
                        case Keys.NumPad8:
                            { 
                                y -= moveSteps;
                            } 
                            break;     // up
                        case Keys.NumPad5:
                            { 
                                y += moveSteps;
                            } 
                            break;     // down
                        case Keys.NumPad4: 
                            { 
                                x -= moveSteps;
                            } 
                            break;      // left 
                        case Keys.NumPad6: 
                            { 
                                x += moveSteps;
                            } 
                            break;      // right
                        case Keys.I:
                            {
                                y -= moveSteps;
                            }
                            break;
                        case Keys.K:
                            {
                                y += moveSteps;
                            }
                            break;
                        case Keys.J:
                            {
                                x -= moveSteps;
                            }
                            break;
                        case Keys.L:
                            {
                                x += moveSteps;
                            }
                            break;
                        case Keys.W: 
                            {
                                h -= moveSteps;
                            } 
                            break;
                        case Keys.S: 
                            {  
                                h += moveSteps;
                            } 
                            break;
                        case Keys.A: 
                            { 
                                w -= moveSteps;
                            } 
                            break;
                        case Keys.D: 
                            { 
                                w += moveSteps;
                            }
                            break;
                        case Keys.Left:
                            preObjectListBoxFile_Click(null, null);
                            return true;
                        case Keys.Right:
                            nextObjectListBoxFile_Click(null, null);
                            return true;
                        case Keys.Up:
                            preFrameListBoxFile_Click(null, null);
                            return true;
                        case Keys.Down:
                            nextFrameListBoxFile_Click(null, null);
                            return true;
                    }

                    // Critical value judgment
                    int widthTmp = pictureBox1.Image.Width;
                    int heightTmp = pictureBox1.Image.Height;

                    if(x >= 0 && y >= 0 && w > 0 && h > 0 && x + w <= widthTmp && y + h <= heightTmp){
                        trackObject.bbox_x = x;
                        trackObject.bbox_y = y;
                        trackObject.bbox_w = w;
                        trackObject.bbox_h = h;
                        SelectedIndexChanged(null, null);
                    }
                }
            }
            return true;
        }

        // 
        private void scaleRect()
        {
            if (selectedFrameIndex != -1 && selectedObjectIndex != -1 && anno.ContainsKey(selectedFrameIndex) && anno[selectedFrameIndex].ContainsKey(selectedObjectIndex))
            {
                TrackObject trackObject = anno[selectedFrameIndex][selectedObjectIndex];
                Rect.Location = new Point(trackObject.bbox_x * zoom_, trackObject.bbox_y * zoom_);
                Rect.Size = new Size(trackObject.bbox_w * zoom_, trackObject.bbox_h * zoom_);
                pictureBox1.Invalidate();
            }
        }

        private void enlarge_Click(object sender, EventArgs e)
        {
            zoom_ *= 2;
            pictureBox1.Width = pictureBox1.Width * 2;
            pictureBox1.Height = pictureBox1.Height * 2;
            ClearSelect();  // Delete previously selected
            scaleRect();
        }

        private void reduce_Click(object sender, EventArgs e)
        {
            if (zoom_ > 1)
            {
                pictureBox1.Width = pictureBox1.Width / 2;
                pictureBox1.Height = pictureBox1.Height / 2;
                zoom_ /= 2;
                scaleRect();  
            }
            if (zoom_ < 1)
            {
                zoom_ = 1;
                scaleRect();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                if(this.checkBox1.Checked) anno[selectedFrameIndex][selectedObjectIndex].is_soc = 1;
                else anno[selectedFrameIndex][selectedObjectIndex].is_soc = 0;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                if(this.checkBox2.Checked) anno[selectedFrameIndex][selectedObjectIndex].is_doc = 1;
                else anno[selectedFrameIndex][selectedObjectIndex].is_doc = 0;
            }
        }
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                if(this.checkBox3.Checked) anno[selectedFrameIndex][selectedObjectIndex].is_moc = 1;
                else anno[selectedFrameIndex][selectedObjectIndex].is_moc = 0;
            }
        }
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                if(this.checkBox4.Checked) anno[selectedFrameIndex][selectedObjectIndex].is_foc = 1;
                else anno[selectedFrameIndex][selectedObjectIndex].is_foc = 0;
            }
        }
        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                if(this.checkBox5.Checked) anno[selectedFrameIndex][selectedObjectIndex].is_mb = 1;
                else anno[selectedFrameIndex][selectedObjectIndex].is_mb = 0;
            }
        }
        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                if(this.checkBox6.Checked) anno[selectedFrameIndex][selectedObjectIndex].is_ov = 1;
                else anno[selectedFrameIndex][selectedObjectIndex].is_ov = 0;
            }
        }

        private void objectTypeTextBox_KeyUp(object sender, EventArgs e)
        {
            if(this.objectTypeTextBox.Text != "" && selectedFrameIndex != -1 && selectedObjectIndex != -1)
            {
                foreach(char c in this.objectTypeTextBox.Text){
                    if(!char.IsNumber(c))
                    {
                        MessageBox.Show("only digits requires");
                        return;
                    }
                }
                anno[selectedFrameIndex][selectedObjectIndex].object_type = Convert.ToInt32(this.objectTypeTextBox.Text);
            }
        }

        private void clothNumTextBox_KeyUp(object sender, EventArgs e)
        {
            if(this.clothNumTextBox.Text != "" && selectedFrameIndex != -1 && selectedObjectIndex != -1)
            {
                foreach(char c in this.clothNumTextBox.Text){
                    if(!char.IsNumber(c))
                    {
                        MessageBox.Show("only digits requires");
                        return;
                    }
                }
                anno[selectedFrameIndex][selectedObjectIndex].cloth_num = Convert.ToInt32(this.clothNumTextBox.Text);
            }
        }

        private void shotTypeTextBox_KeyUp(object sender, EventArgs e)
        {
            if(this.shotTypeTextBox.Text != "" && selectedFrameIndex != -1 && selectedObjectIndex != -1)
            {
                foreach(char c in this.shotTypeTextBox.Text){
                    if(!char.IsNumber(c))
                    {
                        MessageBox.Show("only digits requires");
                        return;
                    }
                }
                anno[selectedFrameIndex][selectedObjectIndex].shot_type = Convert.ToInt32(this.shotTypeTextBox.Text);
            }
        }

        private void reset_Click(object sender, EventArgs e)
        {
            isreset_ = true;
            SelectedIndexChanged(null, null);
        }

        // clear box
        private void clearBox_Click(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                anno[selectedFrameIndex][selectedObjectIndex].bbox_x = 0;
                anno[selectedFrameIndex][selectedObjectIndex].bbox_y = 0;
                anno[selectedFrameIndex][selectedObjectIndex].bbox_w = 0;
                anno[selectedFrameIndex][selectedObjectIndex].bbox_h = 0;
                SelectedIndexChanged(null, null);
            }
        }

        // clear box
        private void clearAllBox_Click(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                anno.Remove(selectedFrameIndex);
                selectedObjectIndex = 0;
                anno[selectedFrameIndex] = new Dictionary<int, TrackObject>();
                anno[selectedFrameIndex][selectedObjectIndex] = new TrackObject(selectedFrameIndex, selectedObjectIndex, 0,0,0,0,0,0,0,0,0,0,0,0,0);
                SelectedIndexChanged(null, null);
            }
        }

        private void save_Click(object sender, EventArgs e)
        {
            if(annoPath_ == "") return;
            File.Delete(annoPath_);
            var content = "";
            var frameKeyCol = anno.Keys;
            foreach(int k1 in frameKeyCol){
                var objectKeyCol = anno[k1].Keys;
                foreach(int k2 in objectKeyCol){
                    var frame_id = anno[k1][k2].frame_id;
                    var object_id = anno[k1][k2].object_id;
                    var x = anno[k1][k2].bbox_x;
                    var y = anno[k1][k2].bbox_y;
                    var w = anno[k1][k2].bbox_w;
                    var h = anno[k1][k2].bbox_h;
                    var object_type = anno[k1][k2].object_type;
                    var cloth_num = anno[k1][k2].cloth_num;
                    var shot_type = anno[k1][k2].shot_type;
                    var is_soc = anno[k1][k2].is_soc;
                    var is_doc = anno[k1][k2].is_doc;
                    var is_moc = anno[k1][k2].is_moc;
                    var is_foc = anno[k1][k2].is_foc;
                    var is_mb = anno[k1][k2].is_mb;
                    var is_ov = anno[k1][k2].is_ov;
                    if((w == 0 || h == 0) && is_ov == 0) continue;
                    else content += Convert.ToString(frame_id) + "," + Convert.ToString(object_id) + "," + Convert.ToString(x) + "," + Convert.ToString(y) + "," + Convert.ToString(w) + "," + Convert.ToString(h) + "," + Convert.ToString(object_type) + "," + Convert.ToString(cloth_num) + "," + Convert.ToString(shot_type) + "," + Convert.ToString(is_soc) +  "," + Convert.ToString(is_doc) + "," + Convert.ToString(is_moc) + "," + Convert.ToString(is_foc) + "," + Convert.ToString(is_mb) + "," + Convert.ToString(is_ov) + "\n";
                }
            }
            File.AppendAllText(annoPath_, content.Trim());
        }

        private void preFrameListBoxFile_Click(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                if(selectedFrameIndex == 0) return;
                selectedFrameIndex -= 1;
                selectedObjectIndex = 0;
                
            }
            SelectedIndexChanged(null, null);
        }

        private void nextFrameListBoxFile_Click(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                if(selectedFrameIndex == frameNum - 1) return;
                selectedFrameIndex += 1;
                selectedObjectIndex = 0;
                if(!anno.ContainsKey(selectedFrameIndex)){
                    anno[selectedFrameIndex] = new Dictionary<int, TrackObject>();
                    foreach(int k in anno[selectedFrameIndex - 1].Keys){
                        anno[selectedFrameIndex][k] = new TrackObject(selectedFrameIndex, k, anno[selectedFrameIndex - 1][k].bbox_x, anno[selectedFrameIndex - 1][k].bbox_y, anno[selectedFrameIndex - 1][k].bbox_w, anno[selectedFrameIndex - 1][k].bbox_h, anno[selectedFrameIndex - 1][k].object_type, anno[selectedFrameIndex - 1][k].cloth_num, anno[selectedFrameIndex - 1][k].shot_type, anno[selectedFrameIndex - 1][k].is_soc,anno[selectedFrameIndex - 1][k].is_doc, anno[selectedFrameIndex - 1][k].is_moc, anno[selectedFrameIndex - 1][k].is_foc, anno[selectedFrameIndex - 1][k].is_mb, anno[selectedFrameIndex - 1][k].is_ov);
                    }
                }
                SelectedIndexChanged(null, null);
            }
        }

        private void preObjectListBoxFile_Click(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                if(selectedObjectIndex == 0) return;
                selectedObjectIndex -= 1;
                SelectedIndexChanged(null, null);
            }
        }

        private void nextObjectListBoxFile_Click(object sender, EventArgs e)
        {
            if(selectedFrameIndex != -1 && selectedObjectIndex != -1){
                selectedObjectIndex += 1;
                SelectedIndexChanged(null, null);
            }
        }
    }
    public class TrackObject
    {
        public int frame_id;
        public int object_id;
        public int bbox_x;
        public int bbox_y;
        public int bbox_w;
        public int bbox_h;
        public int object_type;
        public int cloth_num;
        public int shot_type;
        public int is_soc;
        public int is_doc;
        public int is_moc;
        public int is_foc;
        public int is_mb;
        public int is_ov;
        public TrackObject(int f_id, int o_id, int x, int y, int w, int h, int o_type, int c_num, int s_type, int soc, int doc, int moc, int foc, int mb, int ov)
        {
            frame_id = f_id;
            object_id = o_id;
            bbox_x = x;
            bbox_y = y;
            bbox_w = w;
            bbox_h = h;
            object_type = o_type;
            cloth_num = c_num;
            shot_type = s_type;
            is_soc = soc;
            is_doc = doc;
            is_moc = moc;
            is_foc = foc;
            is_mb = mb;
            is_ov = ov;
        }
    }
}
