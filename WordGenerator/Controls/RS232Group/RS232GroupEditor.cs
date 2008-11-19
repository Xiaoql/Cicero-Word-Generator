using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DataStructures;
using wgControlLibrary;

namespace WordGenerator.Controls.Temporary
{
    public partial class RS232GroupEditor : UserControl
    {
        private RS232Group rs232Group;

        private ChannelCollection rs232ChannelCollection;

        private List<RS232GroupChannelSelection> groupChannelSelectors;

        /// <summary>
        /// This bool is used to stop an infinite loop when selecting an analoggroup from a combobox.
        /// </summary>
        private bool rs232GroupBeingChanged = false;

        private void layoutGroupChannelSelectors()
        {
            foreach (RS232GroupChannelSelection sel in groupChannelSelectors)
            {
                this.groupChannelSelectorPanel.Controls.Remove(sel);
                sel.Dispose();
            }
            groupChannelSelectors.Clear();

            List<int> channelIDs = rs232ChannelCollection.getSortedChannelIDList();

            foreach (int id in channelIDs)
            {
                groupChannelSelectors.Add(new RS232GroupChannelSelection(rs232ChannelCollection.Channels[id], rs232Group.getChannelData(id)));
            }

            for (int i = 0; i < groupChannelSelectors.Count; i++)
            {
                groupChannelSelectors[i].Visible = true;
                if (rs232Group == null)
                    groupChannelSelectors[i].Enabled = false;
                else
                    groupChannelSelectors[i].Enabled = true;
                this.groupChannelSelectorPanel.Controls.Add(groupChannelSelectors[i]);
                groupChannelSelectors[i].Show();
                groupChannelSelectors[i].updateGUI += new EventHandler(groupChannnelSelector_updateGUI);
            }
            this.groupChannelSelectorPanel.Invalidate();
        }

        void groupChannnelSelector_updateGUI(object sender, EventArgs e)
        {
            this.layoutGraphCollection();
        }


        public void setRS232Group(RS232Group rs232Group)
        {

            if (rs232Group == null)
                rs232Group = new RS232Group("Placehold RS232 group. Do not use.");
            this.rs232Group = rs232Group;


            previousObjectBackup = rs232Group;

            this.renameTextBox.Text = rs232Group.GroupName;
            fillSelectorCombobox();
            rs232GroupBeingChanged = true;
            this.rs232GroupSelector.SelectedItem = rs232Group;
            rs232GroupBeingChanged = false;
            layoutGroupChannelSelectors();
            layoutGraphCollection();
            waveformEditor1.setWaveform(null);
            descBox.Text = rs232Group.GroupDescription;

        }

        private void layoutGraphCollection()
        {
            if (WordGenerator.mainClientForm.instance != null)
                WordGenerator.mainClientForm.instance.cursorWait();

            List<Waveform> waveformsToDisplay = new List<Waveform>();
            List<string> channelNamesToDisplay = new List<string>();


            // figure out what to display in the waveform graph
            if (rs232Group != null)
            {
                List<int> usedChannelIDs = rs232Group.getChannelIDs();
                for (int id = 0; id < usedChannelIDs.Count; id++)
                {
                    RS232GroupChannelData channelData = rs232Group.ChannelDatas[id];
                    if (channelData.Enabled)
                    {
                        // if there are graph-based rs232 data types in future, add their waveform display handlers here
                    }
                }
            }


            waveformGraphCollection1.deactivateAllGraphs();

            waveformGraphCollection1.setWaveforms(waveformsToDisplay);
            waveformGraphCollection1.setChannelNames(channelNamesToDisplay);
            waveformGraphCollection1.setWaveformEditor(waveformEditor1);

            if (WordGenerator.mainClientForm.instance != null)
                WordGenerator.mainClientForm.instance.cursorWaitRelease();

        }

        public void setChannelCollection(ChannelCollection gpibChannelCollection)
        {
            this.rs232ChannelCollection = gpibChannelCollection;
            this.layoutGroupChannelSelectors();
        }

        public RS232GroupEditor()
        {
            InitializeComponent();
            groupChannelSelectors = new List<RS232GroupChannelSelection>();
            this.setChannelCollection(new ChannelCollection());
            this.setRS232Group(new RS232Group("Placehold RS232 group. Do not use."));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RS232Group gg = new RS232Group("RS232 Group " + (Storage.sequenceData.RS232Groups.Count + 1));
            Storage.sequenceData.RS232Groups.Add(gg);
            setRS232Group(gg);
        }

        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            fillSelectorCombobox();
        }

        private void fillSelectorCombobox()
        {
            rs232GroupSelector.Items.Clear();
            if (Storage.sequenceData != null)
            {
                foreach (RS232Group gg in Storage.sequenceData.RS232Groups)
                    rs232GroupSelector.Items.Add(gg);
            }

        }

        Object previousObjectBackup;

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!rs232GroupBeingChanged)
            {
                previousObjectBackup = rs232GroupSelector.SelectedItem;
                RS232Group gg = rs232GroupSelector.SelectedItem as RS232Group;
                setRS232Group(gg);
            }
        }

        private void renameTextBox_TextChanged(object sender, EventArgs e)
        {
            //gpibGroup.GroupName = renameTextBox.Text;
        }

        private void renameButton_Click(object sender, EventArgs e)
        {
            RS232Group temp = this.rs232Group;
            if (rs232Group != null)
            {
                rs232Group.GroupName = renameTextBox.Text;
                this.rs232GroupSelector.SelectedItem = null;
                this.rs232GroupSelector.SelectedItem = temp;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (Storage.sequenceData != null)
            {
                if (Storage.sequenceData.RS232Groups.Contains(this.rs232Group))
                {
                    foreach (TimeStep step in Storage.sequenceData.TimeSteps)
                    {
                        if (step.rs232Group == this.rs232Group)
                        {
                            MessageBox.Show("Cannot delete this group, it is used in timestep " + step.ToString());
                            return;
                        }
                    }
                    Storage.sequenceData.RS232Groups.Remove(this.rs232Group);
                    this.rs232GroupSelector.SelectedItem = null;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.rs232Group == null)
            {
                MessageBox.Show("Cannot output null group.");
                return;
            }

            if (!Storage.sequenceData.Lists.ListLocked)
            {
                MessageBox.Show("Lists not locked, unable to output.");
            }

            if (Storage.settingsData.unconnectedRequiredServers().Count != 0)
            {
                string missingServers = ServerManager.convertListOfServersToOneString(Storage.settingsData.unconnectedRequiredServers());
                MessageBox.Show("Unable to output group, the following required servers are missing: " + missingServers);
                return;
            }

            ServerManager.ServerActionStatus status = Storage.settingsData.serverManager.outputRS232GroupOnConnectedServers(rs232Group, Storage.settingsData, null);
            if (status != ServerManager.ServerActionStatus.Success)
            {
                MessageBox.Show("Failed due to server error or disconnection.");
                return;
            }

            MessageBox.Show("Group " + rs232Group.ToString() + " output successfully.");
        }

        private void descBox_TextChanged(object sender, EventArgs e)
        {
            if (this.rs232Group != null)
            {
                rs232Group.GroupDescription = descBox.Text;
            }
        }

        private void plus_Click(object sender, EventArgs e)
        {
            if (rs232GroupSelector.SelectedIndex < rs232GroupSelector.Items.Count - 1)
            {
                rs232GroupSelector.SelectedIndex++;

            }


        }

        private void minus_Click(object sender, EventArgs e)
        {
            if (rs232GroupSelector.SelectedIndex > 0)
            {
                rs232GroupSelector.SelectedIndex--;
            }
        }

        private void rs232GroupSelector_DropDownClosed(object sender, EventArgs e)
        {
            if (rs232GroupSelector.SelectedItem == null)
            {
                rs232GroupSelector.SelectedItem = previousObjectBackup as RS232Group;
            }
        }



        private List<Label> runOrderLabels;
        private Dictionary<Label, RS232Group> runOrderLabelGroups;
        public void updateRunOrderPanel()
        {
            if (runOrderLabels != null)
            {
                foreach (Label lab in runOrderLabels)
                {
                    runOrderPanel.Controls.Remove(lab);
                    lab.Dispose();
                }
                runOrderLabels.Clear();
                runOrderLabelGroups.Clear();
            }
            else
            {
                runOrderLabels = new List<Label>();
                runOrderLabelGroups = new Dictionary<Label, RS232Group>();
            }

            int xPos = label2.Location.X + label2.Width;
            if (Storage.sequenceData != null)
            {
                if (Storage.sequenceData.TimeSteps != null)
                {
                    foreach (TimeStep step in Storage.sequenceData.TimeSteps)
                    {
                        if (step.StepEnabled)
                        {
                            if (step.rs232Group != null)
                            {
                                RS232Group rg = step.rs232Group;
                                Label lab = new Label();
                                lab.Text = rg.ToString();
                                lab.BorderStyle = BorderStyle.FixedSingle;
                                lab.AutoSize = false;
                                lab.Width = 80;
                                lab.TextAlign = ContentAlignment.MiddleCenter;
                                lab.AutoEllipsis = true;
                                lab.Location = new Point(xPos, label2.Location.Y);
                                lab.Click += new EventHandler(runOrderLabelClick);
                                runOrderLabelGroups.Add(lab, rg);
                                runOrderLabels.Add(lab);

                                

                                this.toolTip1.SetToolTip(lab, "Timestep: " + step.StepName + ", Duration: " + step.StepDuration.ToString());

                                xPos += lab.Width + 10;
                            }
                        }
                    }
                }
            }

            runOrderPanel.Controls.AddRange(runOrderLabels.ToArray());

        }

        void runOrderLabelClick(object sender, EventArgs e)
        {
            Label lab = sender as Label;
            if (runOrderLabelGroups.ContainsKey(lab))
            {
                this.setRS232Group(runOrderLabelGroups[lab]);
            }
        }

        private void RS232GroupEditor_VisibleChanged(object sender, EventArgs e)
        {
            updateRunOrderPanel();
        }

    }
}