using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ME3Server_WV
{
    public partial class GUI_PacketEditor : Form
    {
        public List<Blaze.Packet> Packets;
        public List<Blaze.Tdf> inlist;
        public int inlistcount;
        public int lastsearchtype = -1;
        public int lastsearch;

        public GUI_PacketEditor()
        {
            InitializeComponent();
        }

        private void openBINToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.bin|*.bin";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    MemoryStream m = new MemoryStream(File.ReadAllBytes(d.FileName));
                    Packets = Blaze.FetchAllBlazePackets(m);
                    RefreshStuff();
                    this.Text = "Packet Viewer - " + Path.GetFileName(d.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:\n" + ex.Message);
                    Packets = null;
                }
            }
        }

        public void RefreshStuff()
        {
            if (Packets == null)
                return;
            listBox1.Items.Clear();
            int count = 0;
            foreach (Blaze.Packet p in Packets)
            {
                string s = (count++).ToString() + " : ";
                s += p.Length.ToString("X4") + " ";
                s += p.Component.ToString("X4") + " ";
                s += p.Command.ToString("X4") + " ";
                s += p.Error.ToString("X4") + " ";
                s += p.QType.ToString("X4") + " ";
                s += p.ID.ToString("X4") + " ";
                s += p.extLength.ToString("X4") + " ";
                byte qtype = (byte)(p.QType >> 8);
                switch (qtype)
                {
                    case 0:
                        s += "[Client]";
                        break;
                    case 0x10:
                        s += "[Server]";
                        break;
                    case 0x20:
                        s += "[Server][Async]";
                        break;
                    case 0x30:
                        s += "[Server][Error]";
                        break;
                }
                s += "[INFO] " + Blaze.PacketToDescriber(p);
                listBox1.Items.Add(s);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1) return;
            try
            {
                rtb2.Text = Blaze.HexDump(Blaze.PacketToRaw(Packets[n]));
                treeView1.Nodes.Clear();
                rtb1.Text = "";
                inlist = new List<Blaze.Tdf>();
                inlistcount = 0;
                List<Blaze.Tdf> Fields = Blaze.ReadPacketContent(Packets[n]);
                foreach (Blaze.Tdf tdf in Fields)
                    treeView1.Nodes.Add(TdfToTree(tdf));
            }
            catch (Exception ex)
            {
                rtb1.Text = "Error:\n" + ex.Message;
            }
        }

        private TreeNode TdfToTree(Blaze.Tdf tdf)
        {            
            TreeNode t, t2, t3;
            switch (tdf.Type)
                {
                    case 3:
                        t = tdf.ToTree();
                        Blaze.TdfStruct str = (Blaze.TdfStruct)tdf;
                        if (str.startswith2)
                            t.Text += " (Starts with 2)";
                        foreach (Blaze.Tdf td in str.Values)
                            t.Nodes.Add(TdfToTree(td));
                        t.Name = (inlistcount++).ToString();
                        inlist.Add(tdf);
                        return t;
                    case 4:
                        t = tdf.ToTree();
                        Blaze.TdfList l = (Blaze.TdfList)tdf;
                        if (l.SubType == 3)
                        {
                            List<Blaze.TdfStruct> l2 = (List<Blaze.TdfStruct>)l.List;
                            for (int i = 0; i < l2.Count; i++)
                            {
                                t2 = new TreeNode("Entry #" + i);
                                if (l2[i].startswith2)
                                    t2.Text += " (Starts with 2)";
                                List<Blaze.Tdf> l3 = l2[i].Values;
                                for (int j = 0; j < l3.Count; j++)
                                    t2.Nodes.Add(TdfToTree(l3[j]));
                                t.Nodes.Add(t2);
                            }
                        }
                        t.Name = (inlistcount++).ToString();
                        inlist.Add(tdf);
                        return t;
                    case 5:
                        t = tdf.ToTree();
                        Blaze.TdfDoubleList ll = (Blaze.TdfDoubleList)tdf;
                        t2 = new TreeNode("List 1");
                        if (ll.SubType1 == 3)
                        {
                            List<Blaze.TdfStruct> l2 = (List<Blaze.TdfStruct>)ll.List1;
                            for (int i = 0; i < l2.Count; i++)
                            {
                                t3 = new TreeNode("Entry #" + i);
                                if (l2[i].startswith2)
                                    t2.Text += " (Starts with 2)";
                                List<Blaze.Tdf> l3 = l2[i].Values;
                                for (int j = 0; j < l3.Count; j++)
                                    t3.Nodes.Add(TdfToTree(l3[j]));
                                t2.Nodes.Add(t3);
                            }
                            t.Nodes.Add(t2);
                        }
                        t2 = new TreeNode("List 2");
                        if (ll.SubType2 == 3)
                        {
                            List<Blaze.TdfStruct> l2 = (List<Blaze.TdfStruct>)ll.List2;
                            for (int i = 0; i < l2.Count; i++)
                            {
                                t3 = new TreeNode("Entry #" + i);
                                if (l2[i].startswith2)
                                    t2.Text += " (Starts with 2)";
                                List<Blaze.Tdf> l3 = l2[i].Values;
                                for (int j = 0; j < l3.Count; j++)
                                    t3.Nodes.Add(TdfToTree(l3[j]));
                                t2.Nodes.Add(t3);
                            }
                            t.Nodes.Add(t2);
                        }
                        t.Name = (inlistcount++).ToString();
                        inlist.Add(tdf);
                        return t;
                    case 6:
                        t = tdf.ToTree();
                        Blaze.TdfUnion tu = (Blaze.TdfUnion)tdf;
                        if (tu.UnionType != 0x7F)
                        {
                            t.Nodes.Add(TdfToTree(tu.UnionContent));
                        }
                        t.Name = (inlistcount++).ToString();
                        inlist.Add(tdf);
                        return t;
                    default:
                        t = tdf.ToTree();
                        t.Name = (inlistcount++).ToString();
                        inlist.Add(tdf);
                        return t;
                }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode t = e.Node;
            if (t != null && t.Name != "")
            {
                int n = Convert.ToInt32(t.Name);
                Blaze.Tdf tdf = inlist[n];
                string s;
                switch (tdf.Type)
                {
                    case 0:
                        Blaze.TdfInteger ti = (Blaze.TdfInteger)tdf;
                        rtb1.Text = "0x" + ti.Value.ToString("X");
                        if (ti.Label == "IP  ")
                        {
                            rtb1.Text +=  Environment.NewLine + "(" + ME3Server.GetStringFromIP(ti.Value) + ")";
                        }
                        break;
                    case 1:
                        rtb1.Text = ((Blaze.TdfString)tdf).Value.ToString();
                        break;
                    case 2:
                        rtb1.Text = "Length: " + ((Blaze.TdfBlob)tdf).Data.Length.ToString();
                        rtb1.Text += Environment.NewLine + Blaze.HexDump(((Blaze.TdfBlob)tdf).Data);
                        break;
                    case 4:
                        Blaze.TdfList l = (Blaze.TdfList)tdf;
                        s = "";
                        for (int i = 0; i < l.Count; i++)
                        {
                            switch (l.SubType)
                            {
                                case 0:
                                    s += "{" + ((List<long>)l.List)[i] + "} ";
                                    break;
                                case 1:
                                    s += "{" + ((List<string>)l.List)[i] + "} ";
                                    break;
                                case 9:
                                    Blaze.TrippleVal t2 =((List<Blaze.TrippleVal>)l.List)[i];
                                    s += "{" + t2.v1.ToString("X") + "; " + t2.v2.ToString("X") + "; " + t2.v3.ToString("X") + "} ";
                                    break;
                            }
                        }
                        rtb1.Text = s;
                        break;
                    case 5:
                        s = "";
                        Blaze.TdfDoubleList ll = (Blaze.TdfDoubleList)tdf;
                        for (int i = 0; i < ll.Count; i++)
                        {
                            s += "{";
                            switch (ll.SubType1)
                            {
                                case 0:
                                    List<long> l1 = (List<long>)ll.List1;
                                    s += l1[i].ToString("X");
                                    break;
                                case 1:                                    
                                    List<string> l2 = (List<string>)ll.List1;
                                    s += l2[i];
                                    break;
                                case 0xA:
                                    List<float> lf1 = (List<float>)ll.List1;
                                    s += lf1[i].ToString();
                                    break;
                                default:
                                    s += "(see List1[" + i + "])";
                                    break;
                            }
                            s += " ; ";
                            switch (ll.SubType2)
                            {
                                case 0:
                                    List<long> l1 = (List<long>)ll.List2;
                                    s += l1[i].ToString("X");
                                    break;
                                case 1:
                                    List<string> l2 = (List<string>)ll.List2;
                                    s += l2[i];
                                    break;
                                case 0xA:
                                    List<float> lf1 = (List<float>)ll.List2;
                                    s += lf1[i].ToString();
                                    break;
                                default:
                                    s += "(see List2[" + i + "])";
                                    break;
                            }
                            s += "}\n";
                        }
                        rtb1.Text = s;
                        break;
                    case 6:
                        rtb1.Text = "Type: 0x" + ((Blaze.TdfUnion)tdf).UnionType.ToString("X2");
                        break;
                    case 7:
                        Blaze.TdfIntegerList til = (Blaze.TdfIntegerList)tdf;
                        s = "";
                        for (int i = 0; i < til.Count; i++)
                        {
                            s += til.List[i].ToString("X");
                            if (i < til.Count - 1)
                                s += "; ";
                        }
                        rtb1.Text = s;
                        break;
                    case 8:
                        Blaze.TdfDoubleVal dval = (Blaze.TdfDoubleVal)tdf;
                        rtb1.Text = "0x" + dval.Value.v1.ToString("X") + " 0x" + dval.Value.v2.ToString("X");
                        break;
                    case 9:
                        Blaze.TdfTrippleVal tval = (Blaze.TdfTrippleVal)tdf;
                        rtb1.Text = "0x" + tval.Value.v1.ToString("X") + " 0x" + tval.Value.v2.ToString("X") + " 0x" + tval.Value.v3.ToString("X");
                        break;                    
                    default:
                        rtb1.Text = "";
                        break;
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            string s = toolStripTextBox1.Text.Replace(" ","");
            if (s.Length != 6)
                return;
            string v = s + "00";
            List<byte> tmp = new List<byte>(Blaze.StringToByteArray(v));
            tmp.Reverse();
            uint val = BitConverter.ToUInt32(tmp.ToArray(), 0);
            string label = Blaze.TagToLabel(val);
            toolStripTextBox2.Text = label;
        }

        private void saveRawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1) return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.bin|*.bin";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllBytes(d.FileName, Blaze.PacketToRaw(Packets[n]));
                MessageBox.Show("Done.");
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            byte[] buff = Blaze.Label2Tag(toolStripTextBox2.Text);
            string s = "";
            for (int i = 0; i < 3; i++)
                s += buff[i].ToString("X2") + " ";
            toolStripTextBox1.Text = s;
        }

        private void exportTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.Nodes.Count == 0)
                return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.txt|*.txt";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllText(d.FileName, ReadNodes(0, treeView1.Nodes));
                MessageBox.Show("Done.");
            }
        }

        private string ReadNodes(int tab, TreeNodeCollection t)
        {
            string tb = "";
            for (int i = 0; i < tab; i++)
                tb += "\t";
            string res = "";
            for (int i = 0; i < t.Count; i++)
            {
                TreeNode t2 = t[i];
                res += tb + t2.Text + "\r\n";
                if (t2.Nodes.Count != 0)
                    res += ReadNodes(tab + 1, t2.Nodes);
            }
            return res;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            string s = toolStripTextBox3.Text.Replace(" ","");
            if (s == "")
                return;
            long l = Convert.ToInt64(s, 16);
            MemoryStream m = new MemoryStream();
            Blaze.CompressInteger(l, m);
            m.Seek(0, 0);
            string r = "";
            for (int i = 0; i < m.Length; i++)
                r += ((byte)m.ReadByte()).ToString("X2") + " ";
            toolStripTextBox4.Text = r;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            string s = toolStripTextBox4.Text.Replace(" ", "");
            if (s == "")
                return;
            MemoryStream m = new MemoryStream();
            for (int i = 0; i < s.Length / 2; i++)
                m.WriteByte(Convert.ToByte(s.Substring(i * 2, 2), 16));
            m.Seek(0, 0);
            long l = Blaze.DecompressInteger(m);
            toolStripTextBox3.Text = l.ToString("X");
        }

        private void treeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeToolStripMenuItem.Checked = true;
            rawToolStripMenuItem.Checked = false;
            treeView1.BringToFront();
        }

        private void rawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeToolStripMenuItem.Checked = false;
            rawToolStripMenuItem.Checked = true;
            rtb2.BringToFront();
        }

        private void findNextByComponentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count == 0)
                return;
            string result = Microsoft.VisualBasic.Interaction.InputBox("Please Enter ID in hex", "Find by Component", "9");
            if (result == "")
                return;
            int ID = Convert.ToInt32(result, 16);
            int n = listBox1.SelectedIndex;
            for (int i = n + 1; i < listBox1.Items.Count; i++) 
                if (Packets[i].Component == ID)
                {
                    listBox1.SelectedIndex = i;
                    lastsearch = ID;
                    lastsearchtype = 0;
                    break;
                }
        }

        private void searchAgainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lastsearchtype == -1)
                return;
            if (listBox1.Items.Count == 0)
                return;
            int n = listBox1.SelectedIndex;
            for (int i = n + 1; i < listBox1.Items.Count; i++)
            {
                switch (lastsearchtype)
                {
                    case 0:
                        if (Packets[i].Component == lastsearch)
                        {
                            listBox1.SelectedIndex = i;
                            return;
                        }
                        break;
                    case 1:
                        if (Packets[i].Command == lastsearch)
                        {
                            listBox1.SelectedIndex = i;
                            return;
                        }
                        break;
                }
            }
        }

        private void findNextByCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count == 0)
                return;
            string result = Microsoft.VisualBasic.Interaction.InputBox("Please Enter ID in hex", "Find by Command", "1D");
            if (result == "")
                return;
            int ID = Convert.ToInt32(result, 16);
            int n = listBox1.SelectedIndex;
            for (int i = n + 1; i < listBox1.Items.Count; i++)
                if (Packets[i].Command == ID)
                {
                    listBox1.SelectedIndex = i;
                    lastsearch = ID;
                    lastsearchtype = 1;
                    break;
                }
        }
    }
}
