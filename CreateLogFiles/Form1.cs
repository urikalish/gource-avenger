using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Mercury.TD.Client.Ota.Api;

namespace Avenger
{
    public partial class Form1 : Form
    {
        public AlmHelper AlmHelper { get; set; }
        
        public Form1()
        {
            InitializeComponent();
            Width = 460;
            AlmHelper = new AlmHelper();
            ShowGroupBox(groupBoxConnection);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            AlmHelper.DisposeConnection();
        }

        private void ShowGroupBox(GroupBox groupBox)
        {
            foreach (var gb in Controls)
            {
                if (gb is GroupBox || gb is TextBox)
                {
                    (gb as Control).Visible = false;
                }
            }
            int margin = 10;
            Width = groupBox.Width + 3 * margin;
            Height = groupBox.Height + 5 * margin;
            groupBox.Top = margin;
            groupBox.Left = margin;
            groupBox.Visible = true;
        }

        private void buttonAuthenticateAlmUser_Click(object sender, EventArgs e)
        {
            comboBoxDomains.Items.Clear();
            comboBoxDomains.Text = "";
            comboBoxProjects.Items.Clear();
            comboBoxProjects.Text = "";
            buttonLoginToProject.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            AlmHelper.ConnectToAlmServerByUrl(textBoxAlmServer.Text);
            AlmHelper.AuthenticateAlmUser(AlmHelper.Site, textBoxAlmUser.Text, textBoxAlmPassword.Text, true);
            Cursor.Current = Cursors.Default;
            Application.DoEvents();
            comboBoxDomains.Items.AddRange(AlmHelper.GetDomainNames().ToArray());
            comboBoxDomains.Enabled = comboBoxDomains.Items.Count > 0;
            if (comboBoxDomains.Items.Count == 1)
            {
                comboBoxDomains.SelectedIndex = 0;
            }
        }

        private void comboBoxDomains_SelectedIndexChanged(object sender, EventArgs e)
        {
            var domainName = ((ComboBox) sender).SelectedItem.ToString();
            comboBoxProjects.Items.AddRange(AlmHelper.GetProjectNames(domainName).ToArray());
            comboBoxProjects.Enabled = comboBoxProjects.Items.Count > 0;
            if (comboBoxProjects.Items.Count == 1)
            {
                comboBoxProjects.SelectedIndex = 0;
            }
            else
            {
                for (var i = 0; i < comboBoxProjects.Items.Count; i++)
                {
                    if (comboBoxProjects.Items[i].ToString().Equals("QC"))
                    {
                        comboBoxProjects.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void comboBoxProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonLoginToProject.Enabled = comboBoxProjects.SelectedIndex > -1;
        }

        private void buttonLoginToProject_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            AlmHelper.LoginToAlmProject(comboBoxDomains.Text, comboBoxProjects.Text);
            comboBoxEntities.Items.Add("Defects");
            comboBoxEntities.Items.Add("Requirements");
            //comboBoxEntities.Items.Add(Strings.EntitiesTests);
            ShowGroupBox(groupBoxFilter);
            Cursor.Current = Cursors.Default;
        }

        private void comboBoxEntities_SelectedIndexChanged(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            comboBoxFilter.Items.Clear();
            var txt = ((ComboBox) sender).SelectedItem.ToString();
            var entityType = EntityType.Defect;
            if (txt == "Defects")
                entityType = EntityType.Defect;
            else if (txt == "Requirements")
                entityType = EntityType.Requirement;
            else if (txt == "Tests")
                entityType = EntityType.Test;
            AlmHelper.EntityType = entityType;
            comboBoxFilter.Items.AddRange(AlmHelper.GetFavoriteFilters().ToArray());
            comboBoxFilter.Enabled = comboBoxFilter.Items.Count > 0;
            Cursor.Current = Cursors.Default;
        }

        private void comboBoxFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonFilterDone.Enabled = true;
        }

        private void buttonFilterDone_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            FillPathCombos();
            ShowGroupBox(groupBoxPath);
            Cursor.Current = Cursors.Default;
        }

        private void FillPathCombos()
        {
            var values = AlmHelper.GetEntityFieldNames().ToArray();
            foreach (var c in groupBoxPath.Controls)
            {
                if (c is ComboBox)
                {
                    var comboBox = c as ComboBox;
                    comboBox.Items.Clear();
                    comboBox.Items.Add("");
                    comboBox.Items.AddRange(values);
                }
            }
            if (AlmHelper.EntityType == EntityType.Defect)
            {
                comboBoxSelect.SelectedIndex = GetComboItemIndex(comboBoxSelect, "Summary");
            }
            else if (AlmHelper.EntityType == EntityType.Requirement)
            {
                comboBoxSelect.SelectedIndex = GetComboItemIndex(comboBoxSelect, "Name");
            }
        }

        private void comboBoxPath_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender as ComboBox == null)
                return;
            var senderName = (sender as ComboBox).Name;
            var senderValue = (sender as ComboBox).SelectedItem.ToString();
            var labels = new List<Label>();
            for (int i = 0; i < groupBoxPath.Controls.Count; i++)
            {
                if (groupBoxPath.Controls[i] is Label)
                {
                    labels.Add((Label)groupBoxPath.Controls[i]);
                }
            }
            var combos = new List<ComboBox>();
            for (int i = 0; i < groupBoxPath.Controls.Count; i++)
            {
                if (groupBoxPath.Controls[i] is ComboBox)
                {
                    combos.Add((ComboBox) groupBoxPath.Controls[i]);
                }
            }
            for (int i = 0; i < combos.Count; i++)
            {
                if (combos[i].Name == senderName)
                {
                    for (int j = i+1; j < combos.Count; j++)
                    {
                        combos[j].SelectedIndex = 0;
                        combos[j].Visible = labels[j].Visible = (j == i + 1) && (senderValue != "");
                    }
                    break;
                }
            }
            buttonPathDone.Enabled = comboBoxSelect.SelectedItem.ToString() != "";
        }
        
        private void buttonPathDone_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            FillColorCombos();
            ShowGroupBox(groupBoxColors);
            Cursor.Current = Cursors.Default;
        }

        private void FillColorCombos()
        {
            var values = AlmHelper.GetEntityFieldNames().ToArray();
            foreach (var c in groupBoxColors.Controls)
            {
                if (c is ComboBox)
                {
                    var comboBox = c as ComboBox;
                    comboBox.Items.Clear();
                    comboBox.Items.Add("");
                    comboBox.Items.AddRange(values);
                }
            }
            if (AlmHelper.EntityType == EntityType.Defect)
            {
                comboBoxEventField1.SelectedIndex = GetComboItemIndex(comboBoxEventField1, "Status");
                textBoxEventValue1.Text = "Closed";
                label1EventColor1.BackColor = Color.Black;

                comboBoxEventField2.SelectedIndex = GetComboItemIndex(comboBoxEventField2, "Status");
                textBoxEventValue2.Text = "Fixed";
                label1EventColor2.BackColor = Color.Green;

                comboBoxEventField3.SelectedIndex = GetComboItemIndex(comboBoxEventField3, "Severity");
                textBoxEventValue3.Text = "4-Critical";
                label1EventColor3.BackColor = Color.Red;

                comboBoxEventField4.SelectedIndex = GetComboItemIndex(comboBoxEventField4, "Severity");
                textBoxEventValue4.Text = "3-High";
                label1EventColor4.BackColor = Color.DarkOrange;

                comboBoxEventField5.SelectedIndex = GetComboItemIndex(comboBoxEventField5, "Severity");
                textBoxEventValue5.Text = "2-Medium";
                label1EventColor5.BackColor = Color.Yellow;

                comboBoxEventField6.SelectedIndex = GetComboItemIndex(comboBoxEventField6, "Severity");
                textBoxEventValue6.Text = "1-Low";
                label1EventColor6.BackColor = Color.Silver;

            }
            else if (AlmHelper.EntityType == EntityType.Requirement)
            {
                comboBoxEventField1.SelectedIndex = GetComboItemIndex(comboBoxEventField1, "Backlog Status");
                textBoxEventValue1.Text = "Not Started";
                label1EventColor1.BackColor = Color.Red;

                comboBoxEventField2.SelectedIndex = GetComboItemIndex(comboBoxEventField2, "Backlog Status");
                textBoxEventValue2.Text = "In Development";
                label1EventColor2.BackColor = Color.DarkOrange;

                comboBoxEventField3.SelectedIndex = GetComboItemIndex(comboBoxEventField3, "Backlog Status");
                textBoxEventValue3.Text = "Ready4QA";
                label1EventColor3.BackColor = Color.Yellow;

                comboBoxEventField4.SelectedIndex = GetComboItemIndex(comboBoxEventField4, "Backlog Status");
                textBoxEventValue4.Text = "In Testing";
                label1EventColor4.BackColor = Color.DodgerBlue;

                comboBoxEventField5.SelectedIndex = GetComboItemIndex(comboBoxEventField5, "Backlog Status");
                textBoxEventValue5.Text = "Tested";
                label1EventColor5.BackColor = Color.Green;

                comboBoxEventField6.SelectedIndex = GetComboItemIndex(comboBoxEventField6, "Backlog Status");
                textBoxEventValue6.Text = "Canceled/On Hold/Postponed";
                label1EventColor6.BackColor = Color.Silver;

                comboBoxEventField7.SelectedIndex = GetComboItemIndex(comboBoxEventField6, "Backlog Status");
                textBoxEventValue7.Text = "Done";
                label1EventColor7.BackColor = Color.Green;  
            }
        }

        private int GetComboItemIndex(ComboBox cb, string value)
        {
            for(int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i].ToString().Equals(value))
                {
                    return i;
                }
            }
            return -1;
        }

        private void comboBoxEvent_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonColorsDone.Enabled = comboBoxSelect.SelectedItem.ToString() != "";
        }

        private void labelEventColor_Click(object sender, EventArgs e)
        {
            if (sender as Label == null)
                return;
            colorDialog1.ShowDialog();
            (sender as Label).BackColor = colorDialog1.Color;
        }

        private void buttonColorsDone_Click(object sender, EventArgs e)
        {
            Text = GetMovieTitle();
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            textBoxLogFile.Text = "GourceInputFile_" + DateTime.Now.Ticks + ".log";
            ShowGroupBox(groupBoxLog);
            Cursor.Current = Cursors.Default;
        }

        private void buttonCreateLog_Click(object sender, EventArgs e)
        {
            if (File.Exists(textBoxLogFile.Text))
                File.Delete(textBoxLogFile.Text);
            buttonCreateLog.Enabled = false;
            buttonAbortLog.Enabled = true;
            buttonLogDone.Enabled = false;
            textBoxLogFile.Enabled = false;
            progressBar1.Value = 0;
            backgroundWorkerCreateLogFile.RunWorkerAsync();
        }

        private void backgroundWorkerCreateLogFile_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                AlmHelper.GetEntetiesAfterFilterApplied(comboBoxFilter.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                Text = ex.Message;
            }
            int entityCount = AlmHelper.FilteredEntities.Count;
            if (entityCount == 0)
                return;
            var logFileHelper = new LogFileHelper();
            InitFileLogHelperFromUI(logFileHelper);
            var counter = 0;
            var percentReported = -1;
            foreach (IBaseEntity entity in AlmHelper.FilteredEntities)
            {
                if (backgroundWorkerCreateLogFile.CancellationPending)
                {
                    progressBar1.Value = 0;
                    return;
                }
                try
                {
                    logFileHelper.HandleOneEntity(entity);
                }
                catch(Exception) { }
                counter++;
                var percent = (int) Math.Ceiling(100.0*counter/entityCount);
                if (percent != percentReported)
                {
                    backgroundWorkerCreateLogFile.ReportProgress(percent);
                    percentReported = percent;
                }
            }
            logFileHelper.SortLogFileEntries();
            logFileHelper.SaveEntriesToLogFile(textBoxLogFile.Text);
        }

        private void InitFileLogHelperFromUI(LogFileHelper logFileHelper)
        {
            string pathSelectField;
            List<string> pathGroupByFields;
            GetPathFields(out pathSelectField, out pathGroupByFields);
            foreach (var pgbf in pathGroupByFields)
                logFileHelper.PathFields.Add(pgbf);
            logFileHelper.PathFields.Add(pathSelectField);
            for (var i = 1; i <= 10; i++)
            {
                var fieldComboBox = groupBoxColors.Controls.Find("comboBoxEventField" + i, false)[0] as ComboBox;
                if (fieldComboBox == null || fieldComboBox.SelectedIndex < 1)
                    continue;
                var valueTextBox = groupBoxColors.Controls.Find("textBoxEventValue" + i, false)[0] as TextBox;
                if (valueTextBox == null || valueTextBox.Text == "")
                    continue;
                var colorLabel = groupBoxColors.Controls.Find("label1EventColor" + i, false)[0] as Label;
                if (colorLabel == null)
                    continue;
                var fieldValue = fieldComboBox.SelectedItem + "|" + valueTextBox.Text;
                logFileHelper.OrderedFieldsValuesForColoring.Add(fieldValue);
                logFileHelper.MapFieldValueToColor.Add(fieldValue, colorLabel.BackColor.R.ToString("X2") + colorLabel.BackColor.G.ToString("X2") + colorLabel.BackColor.B.ToString("X2"));
            }
        }

        private void GetPathFields(out string pathSelectField, out List<string> pathGroupByFields)
        {
            pathSelectField = "";
            pathGroupByFields = new List<string>();

            var gbs = new List<ComboBox>();
            foreach (var c in groupBoxPath.Controls)
            {
                if (c as ComboBox != null)
                {
                    gbs.Add(c as ComboBox);
                }
            }
            for (int i = 1; i < gbs.Count; i++)
            {
                var cb = gbs[i];
                if (cb != null && cb.SelectedIndex > 0)
                {
                    pathGroupByFields.Add(cb.SelectedItem.ToString());
                }
            }
            if (gbs[0] != null && gbs[0].SelectedIndex > 0)
            {
                pathSelectField = gbs[0].SelectedItem.ToString();
            }    
        }

        private void buttonAbortLog_Click(object sender, EventArgs e)
        {
            buttonAbortLog.Enabled = false;
            buttonLogDone.Enabled = false;
            backgroundWorkerCreateLogFile.CancelAsync();           
        }
        
        private void backgroundWorkerCreateLogFile_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorkerCreateLogFile_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //buttonCreateLog.Enabled = progressBar1.Value != 100;
            textBoxLogFile.Enabled = true;
            buttonCreateLog.Enabled = true;
            buttonAbortLog.Enabled = false;
            buttonLogDone.Enabled = progressBar1.Value == 100;
        }

        private void buttonLogDone_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            textBoxGourceCmdArgs.Text =
                @"-a 1 -s 1 -c 1 -e 0.1 --disable-auto-rotate --user-image-dir images -i 0 -b 000000 --background-image gray.jpg --bloom-multiplier 0.1 --bloom-intensity 0.1 --hide filenames --title """ +
                GetMovieTitle() + @""" " + textBoxLogFile.Text;
            ShowGroupBox(groupBoxGource);
            Cursor.Current = Cursors.Default;
        }

        private string GetMovieTitle()
        {
            var sb = new StringBuilder();
            sb.Append("ALM");
            switch (AlmHelper.EntityType)
            {
                case EntityType.Defect:
                    sb.Append(" Defect");
                    break;
                case EntityType.Requirement:
                    sb.Append(" Requirement");
                    break;
            }
            string pathSelectField;
            List<string> pathGroupByFields;
            GetPathFields(out pathSelectField, out pathGroupByFields);
            sb.Append(" " + pathSelectField);
            if (pathGroupByFields.Count > 0)
            {
                sb.Append(" grouped by");
                for (var i = 0; i < pathGroupByFields.Count; i++)
                {
                    sb.Append((i == 0 ? " " : ", ") + pathGroupByFields[i]);    
                }
            }
            sb.Append(". Filter:" + comboBoxFilter.SelectedItem);
            return sb.ToString();
        }

        private void buttonGourceDone_Click(object sender, EventArgs e)
        {
            var pProcess = new Process
            {
                StartInfo =
                {
                    FileName = "gource.exe",
                    Arguments = textBoxGourceCmdArgs.Text,
                    WindowStyle = ProcessWindowStyle.Maximized
                }
            };
            pProcess.Start();
    
        }
    }
}
