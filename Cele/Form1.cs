Panel btnPanel = new Panel { Height = 50, Dock = DockStyle.Bottom };
            Button btnRestore = new Button
            {
                Text = "Restore Selected",
                Location = new Point(10, 10),
                Size = new Size(120, 30)
            };

            btnRestore.Click += (s, ev) =>
            {
                if (lstHidden.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select an artist to restore.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string artist = (string)lstHidden.SelectedItems[0].Tag;
                hiddenArtists.Remove(artist);
                SaveHiddenArtists();
                LoadAllTracks();
                LoadArtists();
                lstHidden.Items.Remove(lstHidden.SelectedItems[0]);
                MessageBox.Show(artist + " has been restored!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            Button btnClose = new Button
            {
                Text = "Close",
                Location = new Point(140, 10),
                Size = new Size(80, 30)
            };
            btnClose.Click += (s, ev) => hiddenForm.Close();

            btnPanel.Controls.Add(btnRestore);
            btnPanel.Controls.Add(btnClose);

            hiddenForm.Controls.Add(lstHidden);
            hiddenForm.Controls.Add(btnPanel);
            hiddenForm.ShowDialog();
        }

        // ================= MUSIC SCAN =================
        private void ScanMusic(string folder)
        {
            if (!Directory.Exists(folder)) return;

            foreach (string file in Directory.GetFiles(folder, "*.mp3", SearchOption.AllDirectories))
            {
                try
                {
                    var tagFile = TagLib.File.Create(file);
                    Image cover = null;
                    if (tagFile.Tag.Pictures.Length > 0)
                    {
                        byte[] data = tagFile.Tag.Pictures[0].Data.Data;
                        using (MemoryStream ms = new MemoryStream(data))
                            cover = Image.FromStream(ms);
                    }
                    tracks.Add(new Track
                    {
                        FilePath = file,
                        Title = tagFile.Tag.Title ?? Path.GetFileNameWithoutExtension(file),
                        Artist = tagFile.Tag.FirstPerformer ?? "Unknown Artist",
                        Album = tagFile.Tag.Album ?? "Unknown Album",
                        Genre = tagFile.Tag.FirstGenre ?? "Unknown Genre",
                        Year = (int)tagFile.Tag.Year,
                        TrackNumber = (int)tagFile.Tag.Track,
                        Duration = tagFile.Properties.Duration,
                        Cover = cover
                    });
                }
                catch { }
            }
        }

        // ================= LOAD LISTS =================
        private void LoadAllTracks()
        {
            listView.Items.Clear();
            foreach (Track t in tracks.Where(t => !hiddenArtists.Contains(GetMergedArtist(t.Artist))).OrderBy(t => t.Title))
            {
                ListViewItem item = new ListViewItem(t.Title);
                item.SubItems.Add(GetMergedArtist(t.Artist));
                item.SubItems.Add(t.Album);
                item.Tag = t;
                listView.Items.Add(item);
            }
        }

        private void LoadArtists()
        {
            listViewArtists.Items.Clear();
            artistImageList.Images.Clear();
            int idx = 0;

            // Group by merged artist
            var artistGroups = tracks.GroupBy(t => GetMergedArtist(t.Artist))
                                    .Where(g => !hiddenArtists.Contains(g.Key))
                                    .OrderBy(g => g.Key);

            foreach (var group in artistGroups)
            {
                string artist = group.Key;
                ArtistInfo info = GetArtistInfo(artist);
                Image artistImage = null;

                // Try custom image first
                if (!string.IsNullOrEmpty(info.CustomImage) && System.IO.File.Exists(info.CustomImage))
                {
                    try
                    {
                        artistImage = Image.FromFile(info.CustomImage);
                    }
                    catch { }
                }

                // Fall back to random album cover from any track in group
                if (artistImage == null)
                {
                    var artistTracks = group.Where(t => t.Cover != null).ToList();
                    if (artistTracks.Any())
                    {
                        var rnd = new Random(artist.GetHashCode());
                        artistImage = artistTracks[rnd.Next(artistTracks.Count)].Cover;
                    }
                }

                // Gray placeholder if nothing available
                if (artistImage == null)
                {
                    Bitmap bmp = new Bitmap(64, 64);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Gray);
                    artistImage = bmp;
                }

                artistImageList.Images.Add(artistImage);

                ListViewItem item = new ListViewItem(artist, idx);
                item.Tag = artist;
                listViewArtists.Items.Add(item);
                idx++;
            }
        }

        private void LoadAlbums()
        {
            listViewAlbums.Items.Clear();
            albumImageList.Images.Clear();
            int idx = 0;

            foreach (var album in tracks.Select(t => t.Album).Distinct().OrderBy(a => a))
            {
                Track t = tracks.First(tk => tk.Album == album);
                AlbumInfo info = GetAlbumInfo(album);

                Image albumCover = null;

                // Try custom cover first
                if (!string.IsNullOrEmpty(info.CustomCover) && System.IO.File.Exists(info.CustomCover))
                {
                    try
                    {
                        albumCover = Image.FromFile(info.CustomCover);
                    }
                    catch { }
                }

                // Fall back to track cover
                if (albumCover == null)
                {
                    albumCover = t.Cover;
                }

                // Gray placeholder
                if (albumCover == null)
                {
                    Bitmap bmp = new Bitmap(64, 64);
                    using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.Gray);
                    albumCover = bmp;
                }

                albumImageList.Images.Add(albumCover);

                ListViewItem item = new ListViewItem(album, idx);
                item.Tag = album;
                listViewAlbums.Items.Add(item);
                idx++;
            }
        }

        // ================= TAB CHANGE =================
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Hide all info panels
            trackInfoPanel.Visible = false;
            albumInfoPanel.Visible = false;
            artistInfoPanel.Visible = false;
        }

        // ================= SELECTION CHANGED =================
        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
            {
                trackInfoPanel.Visible = false;
                return;
            }

            Track t = (Track)listView.SelectedItems[0].Tag;

            trackCoverPic.Image = t.Cover;
            lblTrackTitle.Text = t.Title;
            lblTrackArtist.Text = GetMergedArtist(t.Artist);
            lblTrackAlbum.Text = t.Album;
            lblTrackDuration.Text = FormatTime(t.Duration.TotalSeconds);
            trackInfoPanel.Visible = true;
        }

        private void ListViewAlbums_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewAlbums.SelectedItems.Count == 0)
            {
                albumInfoPanel.Visible = false;
                return;
            }

            string album = (string)listViewAlbums.SelectedItems[0].Tag;
            AlbumInfo info = GetAlbumInfo(album);
            currentAlbumTracks = tracks.Where(t => t.Album == album)
                                      .OrderBy(t => t.TrackNumber)
                                      .ThenBy(t => t.Title)
                                      .ToList();

            var first = currentAlbumTracks.First();

            // Use custom cover if available
            if (!string.IsNullOrEmpty(info.CustomCover) && System.IO.File.Exists(info.CustomCover))
            {
                try
                {
                    albumCoverPic.Image = Image.FromFile(info.CustomCover);
                }
                catch
                {
                    albumCoverPic.Image = first.Cover;
                }
            }
            else
            {
                albumCoverPic.Image = first.Cover;
            }

            lblAlbumTitle.Text = first.Album;
            lblAlbumArtist.Text = string.IsNullOrEmpty(info.CustomArtist) ? GetMergedArtist(first.Artist) : info.CustomArtist;
            lblAlbumGenre.Text = first.Genre;
            lblAlbumType.Text = info.Type;

            double totalSeconds = currentAlbumTracks.Sum(tr => tr.Duration.TotalSeconds);
            TimeSpan ts = TimeSpan.FromSeconds(totalSeconds);
            lblAlbumDuration.Text = string.Format("{0:D2}:{1:D2}", (int)ts.TotalMinutes, ts.Seconds);

            btnAlbumPrefer.BackColor = info.PreferenceBoost > 0 ? Color.LightGreen : SystemColors.Control;
            albumInfoPanel.Visible = true;
        }

        private void ListViewArtists_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewArtists.SelectedItems.Count == 0)
            {
                artistInfoPanel.Visible = false;
                return;
            }

            string artist = (string)listViewArtists.SelectedItems[0].Tag;
            ArtistInfo info = GetArtistInfo(artist);

            // Update artist card
            artistCoverPic.Image = artistImageList.Images[listViewArtists.SelectedItems[0].ImageIndex];
            lblArtistName.Text = artist;
            lblArtistBorn.Text = string.IsNullOrEmpty(info.Born) ? "" : "Born: " + info.Born;

            var artistAlbums = tracks.Where(t => GetMergedArtist(t.Artist) == artist).Select(t => t.Album).Distinct().Count();
            lblArtistReleases.Text = "Releases: " + artistAlbums.ToString();

            // Most played (placeholder - just show first album for now)
            var mostPlayed = tracks.Where(t => GetMergedArtist(t.Artist) == artist).Select(t => t.Album).FirstOrDefault();
            lblArtistMostPlayed.Text = "Most played release: " + (mostPlayed ?? "N/A");

            // Update prefer button
            btnArtistPrefer.BackColor = info.PreferenceBoost > 0 ? Color.LightGreen : SystemColors.Control;
            artistInfoPanel.Visible = true;
        }

        // ================= PLAYBACK =================
        private void PlayTrack(int index)
        {
            if (index < 0 || index >= tracks.Count) return;
            currentIndex = index;
            Track t = tracks[index];

            player.URL = t.FilePath;
            player.Ctlcontrols.play();
            btnPlay.Text = "Pause"; isPlaying = true;

            lblTitle.Text = t.Title;
            lblArtistAlbum.Text = GetMergedArtist(t.Artist) + " - " + t.Album;
            picCover.Image = t.Cover;
        }

        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (currentIndex == -1) return;

            if (player.Ctlcontrols.currentPosition > PREV_RESTART_THRESHOLD)
                player.Ctlcontrols.currentPosition = 0;
            else if (currentIndex > 0)
                PlayTrack(currentIndex - 1);
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            if (player.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                player.Ctlcontrols.pause();
                btnPlay.Text = "Play"; isPlaying = false;
            }
            else
            {
                if (currentIndex == -1 && tracks.Count > 0)
                    PlayTrack(0);
                else
                    player.Ctlcontrols.play();
                btnPlay.Text = "Pause"; isPlaying = true;
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (currentIndex == -1) return;

            string album = tracks[currentIndex].Album;
            var albumTracks = tracks.Where(t => t.Album == album)
                                   .OrderBy(t => t.TrackNumber)
                                   .ThenBy(t => t.Title)
                                   .ToList();
            if (albumTracks.Count == 0) return;

            PlayTrack(tracks.IndexOf(albumTracks[0]));
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (currentIndex < tracks.Count - 1)
                PlayTrack(currentIndex + 1);
        }

        private void Player_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState == (int)WMPLib.WMPPlayState.wmppsStopped && isPlaying)
            {
                BtnNext_Click(null, null);
            }
            else if (e.newState == (int)WMPLib.WMPPlayState.wmppsPlaying)
            {
                btnPlay.Text = "Pause";
                isPlaying = true;
            }
            else if (e.newState == (int)WMPLib.WMPPlayState.wmppsPaused)
            {
                btnPlay.Text = "Play";
                isPlaying = false;
            }
        }

        // ================= SHUFFLE =================
        private Track GetWeightedRandomTrack()
        {
            List<Track> weightedTracks = new List<Track>();

            foreach (Track t in tracks.Where(tr => !hiddenArtists.Contains(GetMergedArtist(tr.Artist))))
            {
                string mergedArtist = GetMergedArtist(t.Artist);
                ArtistInfo artistInfo = GetArtistInfo(mergedArtist);
                AlbumInfo albumInfo = GetAlbumInfo(t.Album);
                int weight = 10 + artistInfo.PreferenceBoost + albumInfo.PreferenceBoost;
                for (int i = 0; i < weight; i++)
                    weightedTracks.Add(t);
            }

            if (weightedTracks.Count == 0) return null;

            Random rnd = new Random();
            return weightedTracks[rnd.Next(weightedTracks.Count)];
        }

        private void BtnShuffleAll_Click(object sender, EventArgs e)
        {
            Track t = GetWeightedRandomTrack();
            if (t != null)
                PlayTrack(tracks.IndexOf(t));
        }

        // ================= LISTVIEW DOUBLE-CLICK =================
        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0) return;
            Track t = (Track)listView.SelectedItems[0].Tag;
            PlayTrack(tracks.IndexOf(t));
        }

        private void ListViewArtists_DoubleClick(object sender, EventArgs e)
        {
            if (listViewArtists.SelectedItems.Count == 0) return;
            string artist = (string)listViewArtists.SelectedItems[0].Tag;
            listView.Items.Clear();
            foreach (Track t in tracks.Where(t => GetMergedArtist(t.Artist) == artist).OrderBy(t => t.Album).ThenBy(t => t.TrackNumber))
            {
                ListViewItem item = new ListViewItem(t.Title);
                item.SubItems.Add(GetMergedArtist(t.Artist));
                item.SubItems.Add(t.Album);
                item.Tag = t;
                listView.Items.Add(item);
            }
            tabControl.SelectedTab = tabTracks;
        }

        private void ListViewAlbums_DoubleClick(object sender, EventArgs e)
        {
            if (listViewAlbums.SelectedItems.Count == 0) return;
            string album = (string)listViewAlbums.SelectedItems[0].Tag;
            currentAlbumTracks = tracks.Where(t => t.Album == album)
                                      .OrderBy(t => t.TrackNumber)
                                      .ThenBy(t => t.Title)
                                      .ToList();

            listView.Items.Clear();
            foreach (Track t in currentAlbumTracks)
            {
                ListViewItem item = new ListViewItem(t.Title);
                item.SubItems.Add(GetMergedArtist(t.Artist));
                item.SubItems.Add(t.Album);
                item.Tag = t;
                listView.Items.Add(item);
            }
            tabControl.SelectedTab = tabTracks;
        }

        // ================= ALBUM BUTTONS =================
        private void BtnAlbumPlay_Click(object sender, EventArgs e)
        {
            if (currentAlbumTracks.Count == 0) return;
            PlayTrack(tracks.IndexOf(currentAlbumTracks[0]));
        }

        private void BtnAlbumShuffle_Click(object sender, EventArgs e)
        {
            if (currentAlbumTracks.Count == 0) return;
            var rnd = new Random();
            var shuffled = currentAlbumTracks.OrderBy(t => rnd.Next()).ToList();
            PlayTrack(tracks.IndexOf(shuffled[0]));
        }

        private void BtnAlbumEdit_Click(object sender, EventArgs e)
        {
            if (listViewAlbums.SelectedItems.Count == 0) return;
            string album = (string)listViewAlbums.SelectedItems[0].Tag;
            AlbumInfo info = GetAlbumInfo(album);

            Form editForm = new Form
            {
                Text = "Edit " + album,
                Width = 450,
                Height = 280,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblCover = new Label { Text = "Cover Art:", Location = new Point(20, 20), AutoSize = true };
            System.Windows.Forms.TextBox txtCover = new System.Windows.Forms.TextBox { Text = info.CustomCover, Location = new Point(120, 18), Width = 200 };
            Button btnBrowseCover = new Button { Text = "Browse", Location = new Point(325, 17), Width = 80 };

            btnBrowseCover.Click += (s, ev) =>
            {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp" };
                if (ofd.ShowDialog() == DialogResult.OK)
                    txtCover.Text = ofd.FileName;
            };

            Label lblArtist = new Label { Text = "Artist:", Location = new Point(20, 60), AutoSize = true };
            System.Windows.Forms.TextBox txtArtist = new System.Windows.Forms.TextBox { Text = info.CustomArtist, Location = new Point(120, 58), Width = 285 };

            Label lblType = new Label { Text = "Type:", Location = new Point(20, 100), AutoSize = true };
            ComboBox cmbType = new ComboBox
            {
                Location = new Point(120, 98),
                Width = 285,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbType.Items.AddRange(new object[] { "Album", "Mixtape", "EP", "Compilation" });
            cmbType.SelectedItem = info.Type;

            Label lblYear = new Label { Text = "Year:", Location = new Point(20, 140), AutoSize = true };
            System.Windows.Forms.TextBox txtYear = new System.Windows.Forms.TextBox { Text = info.Year, Location = new Point(120, 138), Width = 100 };

            Button btnSave = new Button { Text = "Save", Location = new Point(200, 190), Width = 80 };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(290, 190), Width = 80 };

            btnSave.Click += (s, ev) =>
            {
                info.CustomCover = txtCover.Text;
                info.CustomArtist = txtArtist.Text;
                info.Type = cmbType.SelectedItem != null ? cmbType.SelectedItem.ToString() : "Album";
                info.Year = txtYear.Text;
                SaveAlbumData();
                LoadAlbums();
                editForm.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += (s, ev) => editForm.DialogResult = DialogResult.Cancel;

            editForm.Controls.Add(lblCover);
            editForm.Controls.Add(txtCover);
            editForm.Controls.Add(btnBrowseCover);
            editForm.Controls.Add(lblArtist);
            editForm.Controls.Add(txtArtist);
            editForm.Controls.Add(lblType);
            editForm.Controls.Add(cmbType);
            editForm.Controls.Add(lblYear);
            editForm.Controls.Add(txtYear);
            editForm.Controls.Add(btnSave);
            editForm.Controls.Add(btnCancel);

            if (editForm.ShowDialog() == DialogResult.OK)
            {
                ListViewAlbums_SelectedIndexChanged(null, null);
            }
        }

        private void BtnAlbumPrefer_Click(object sender, EventArgs e)
        {
            if (listViewAlbums.SelectedItems.Count == 0) return;
            string album = (string)listViewAlbums.SelectedItems[0].Tag;
            AlbumInfo info = GetAlbumInfo(album);

            if (info.PreferenceBoost > 0)
            {
                info.PreferenceBoost = 0;
                btnAlbumPrefer.BackColor = SystemColors.Control;
                MessageBox.Show(album + " removed from preferences.", "Preferism", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                info.PreferenceBoost = 10;
                btnAlbumPrefer.BackColor = Color.LightGreen;
                MessageBox.Show(album + " now has +10% boost in shuffle!\n\nWhen shuffling all tracks, this album's songs are more likely to play.", "Preferism (Beta)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            SaveAlbumData();
        }

        // ================= ARTIST BUTTONS =================
        private void BtnArtistEdit_Click(object sender, EventArgs e)
        {
            if (listViewArtists.SelectedItems.Count == 0) return;
            string artist = (string)listViewArtists.SelectedItems[0].Tag;
            ArtistInfo info = GetArtistInfo(artist);

            Form editForm = new Form
            {
                Text = "Edit " + artist,
                Width = 400,
                Height = 280,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblBorn = new Label { Text = "Born (year):", Location = new Point(20, 20), AutoSize = true };
            System.Windows.Forms.TextBox txtBorn = new System.Windows.Forms.TextBox { Text = info.Born, Location = new Point(120, 18), Width = 200 };

            Label lblImage = new Label { Text = "Artist Image:", Location = new Point(20, 60), AutoSize = true };
            System.Windows.Forms.TextBox txtImage = new System.Windows.Forms.TextBox { Text = info.CustomImage, Location = new Point(120, 58), Width = 150 };
            Button btnBrowse = new Button { Text = "Browse", Location = new Point(275, 57), Width = 60 };

            btnBrowse.Click += (s, ev) =>
            {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp" };
                if (ofd.ShowDialog() == DialogResult.OK)
                    txtImage.Text = ofd.FileName;
            };

            Label lblMerge = new Label { Text = "Merge with:", Location = new Point(20, 100), AutoSize = true };
            ComboBox cmbMerge = new ComboBox
            {
                Location = new Point(120, 98),
                Width = 215,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbMerge.Items.Add("(none)");
            foreach (string otherArtist in tracks.Select(t => t.Artist).Distinct().OrderBy(a => a))
            {
                if (otherArtist != artist)
                    cmbMerge.Items.Add(otherArtist);
            }

            if (artistMerges.ContainsKey(artist))
                cmbMerge.SelectedItem = artistMerges[artist];
            else
                cmbMerge.SelectedIndex = 0;

            Label lblMergeInfo = new Label
            {
                Text = "Merging combines this artist with another\n(useful for collaborations like 'Artist feat. Other')",
                Location = new Point(20, 130),
                Size = new Size(350, 40),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8f)
            };

            Button btnSave = new Button { Text = "Save", Location = new Point(120, 190), Width = 80 };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(210, 190), Width = 80 };

            btnSave.Click += (s, ev) =>
            {
                info.Born = txtBorn.Text;
                info.CustomImage = txtImage.Text;

                // Handle merge
                if (cmbMerge.SelectedItem != null && cmbMerge.SelectedItem.ToString() != "(none)")
                {
                    artistMerges[artist] = cmbMerge.SelectedItem.ToString();
                }
                else
                {
                    if (artistMerges.ContainsKey(artist))
                        artistMerges.Remove(artist);
                }

                SaveArtistData();
                SaveArtistMerges();
                LoadArtists();
                LoadAllTracks();
                editForm.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += (s, ev) => editForm.DialogResult = DialogResult.Cancel;

            editForm.Controls.Add(lblBorn);
            editForm.Controls.Add(txtBorn);
            editForm.Controls.Add(lblImage);
            editForm.Controls.Add(txtImage);
            editForm.Controls.Add(btnBrowse);
            editForm.Controls.Add(lblMerge);
            editForm.Controls.Add(cmbMerge);
            editForm.Controls.Add(lblMergeInfo);
            editForm.Controls.Add(btnSave);
            editForm.Controls.Add(btnCancel);

            if (editForm.ShowDialog() == DialogResult.OK)
            {
                ListViewArtists_SelectedIndexChanged(null, null);
            }
        }

        private void BtnArtistPrefer_Click(object sender, EventArgs e)
        {
            if (listViewArtists.SelectedItems.Count == 0) return;
            string artist = (string)listViewArtists.SelectedItems[0].Tag;
            ArtistInfo info = GetArtistInfo(artist);

            if (info.PreferenceBoost > 0)
            {
                info.PreferenceBoost = 0;
                btnArtistPrefer.BackColor = SystemColors.Control;
                MessageBox.Show(artist + " removed from preferences.", "Preferism", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                info.PreferenceBoost = 10;
                btnArtistPrefer.BackColor = Color.LightGreen;
                MessageBox.Show(artist + " now has +10% boost in shuffle!\n\nWhen shuffling all tracks, this artist's songs are more likely to play.", "Preferism (Beta)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            SaveArtistData();
        }

        private void BtnArtistDelete_Click(object sender, EventArgs e)
        {
            if (listViewArtists.SelectedItems.Count == 0) return;
            string artist = (string)listViewArtists.SelectedItems[0].Tag;

            DialogResult result = MessageBox.Show(
                "Hide " + artist + " from your library?\n\nThis won't delete their music files, just hide them from the app.",
                "Hide Artist",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                hiddenArtists.Add(artist);
                SaveHiddenArtists();
                LoadAllTracks();
                LoadArtists();
                artistInfoPanel.Visible = false;
                MessageBox.Show(artist + " has been hidden.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ================= PROGRESS =================
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (player.currentMedia == null) return;
            double duration = player.currentMedia.duration;
            double position = player.Ctlcontrols.currentPosition;
            if (duration > 0)
            {
                progressBar.Maximum = (int)duration;
                progressBar.Value = Math.Min((int)position, progressBar.Maximum);
                lblTime.Text = FormatTime(position) + " / " + FormatTime(duration);
            }
        }

        private void ProgressBar_MouseClick(object sender, MouseEventArgs e)
        {
            if (player.currentMedia == null) return;
            double duration = player.currentMedia.duration;
            double clickPercent = (double)e.X / progressBar.Width;
            double newPosition = duration * clickPercent;
            player.Ctlcontrols.currentPosition = newPosition;
        }

        private string FormatTime(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return string.Format("{0}:{1:00}", (int)t.TotalMinutes, t.Seconds);
        }
    }

    // ================= DATA CLASSES =================
    public class ArtistInfo
    {
        public string Name { get; set; }
        public string Born { get; set; }
        public string CustomImage { get; set; }
        public int PreferenceBoost { get; set; }
    }

    public class AlbumInfo
    {
        public string Name { get; set; }
        public string CustomCover { get; set; }
        public string CustomArtist { get; set; }
        public string Type { get; set; }
        public string Year { get; set; }
        public int PreferenceBoost { get; set; }
    }

    // ================= CUSTOM AERO PROGRESS BAR =================
    public class AeroProgressBar : Control
    {
        private int _value = 0;
        private int _maximum = 100;

        public int Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(0, Math.Min(value, _maximum));
                Invalidate();
            }
        }

        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = Math.Max(1, value);
                if (_value > _maximum) _value = _maximum;
                Invalidate();
            }
        }

        public AeroProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Background (track)
            using (LinearGradientBrush trackBrush = new LinearGradientBrush(
                rect, Color.FromArgb(200, 210, 220), Color.FromArgb(230, 235, 240), 90f))
            {
                g.FillRectangle(trackBrush, rect);
            }

            // Border
            using (Pen borderPen = new Pen(Color.FromArgb(140, 160, 180)))
            {
                g.DrawRectangle(borderPen, rect);
            }

            // Progress fill
            if (_value > 0 && _maximum > 0)
            {
                int fillWidth = (int)((double)_value / _maximum * (Width - 2));
                if (fillWidth > 0)
                {
                    Rectangle fillRect = new Rectangle(1, 1, fillWidth, Height - 2);

                    // Aero glass effect
                    using (LinearGradientBrush fillBrush = new LinearGradientBrush(
                        fillRect, Color.FromArgb(100, 180, 255), Color.FromArgb(50, 140, 220), 90f))
                    {
                        g.FillRectangle(fillBrush, fillRect);
                    }

                    // Glossy highlight on top half
                    Rectangle highlightRect = new Rectangle(1, 1, fillWidth, (Height - 2) / 2);
                    using (LinearGradientBrush highlightBrush = new LinearGradientBrush(
                        highlightRect, Color.FromArgb(80, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                    {
                        g.FillRectangle(highlightBrush, highlightRect);
                    }
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AxWMPLib;
using TagLib;

namespace Cele
{
    public partial class Form1 : Form
    {
        // ================= UI =================
        private TabControl tabControl;
        private TabPage tabAlbums;
        private TabPage tabArtists;
        private TabPage tabTracks;
        private TabPage tabAbout;

        private ListView listView;
        private ListView listViewArtists;
        private ListView listViewAlbums;
        private ImageList albumImageList;
        private ImageList artistImageList;

        private PictureBox picCover;
        private Label lblTitle;
        private Label lblArtistAlbum;

        private Panel bottomPanel;
        private Button btnPrev, btnPlay, btnStop, btnNext;
        private AeroProgressBar progressBar;
        private Label lblTime;

        private AxWindowsMediaPlayer player;
        private Timer progressTimer;

        // Album Info Panel
        private Panel albumInfoPanel;
        private PictureBox albumCoverPic;
        private Label lblAlbumTitle, lblAlbumArtist, lblAlbumGenre, lblAlbumDuration, lblAlbumType;
        private Button btnAlbumPlay, btnAlbumShuffle, btnAlbumEdit, btnAlbumPrefer;

        // Track Info Panel
        private Panel trackInfoPanel;
        private PictureBox trackCoverPic;
        private Label lblTrackTitle, lblTrackArtist, lblTrackAlbum, lblTrackDuration;
        private Button btnShuffleAll;

        // Artist Info Panel
        private Panel artistInfoPanel;
        private PictureBox artistCoverPic;
        private Label lblArtistName, lblArtistBorn, lblArtistReleases, lblArtistMostPlayed;
        private Button btnArtistEdit, btnArtistPrefer, btnArtistDelete;

        // ================= Logic =================
        private List<Track> tracks = new List<Track>();
        private int currentIndex = -1;
        private bool isPlaying = false;
        private List<Track> currentAlbumTracks = new List<Track>();
        private Dictionary<string, ArtistInfo> artistData = new Dictionary<string, ArtistInfo>();
        private Dictionary<string, AlbumInfo> albumData = new Dictionary<string, AlbumInfo>();
        private Dictionary<string, string> artistMerges = new Dictionary<string, string>(); // from -> to
        private List<string> hiddenArtists = new List<string>();
        private const int PREV_RESTART_THRESHOLD = 3;

        // ================= Constructor =================
        public Form1()
        {
            InitializeComponent();
            BuildUI();
            LoadArtistData();
            LoadAlbumData();
            LoadHiddenArtists();
            LoadArtistMerges();
            ScanMusic(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            LoadAllTracks();
            LoadArtists();
            LoadAlbums();
        }

        // ================= UI BUILD =================
        private void BuildUI()
        {
            // Form
            Text = "Project Cele";
            Width = 1000;
            Height = 580;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AllowDrop = true;
            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;

            // Tabs
            tabControl = new TabControl { Dock = DockStyle.Top, Height = 420 };
            tabAlbums = new TabPage("Albums");
            tabArtists = new TabPage("Artists");
            tabTracks = new TabPage("All Tracks");
            tabAbout = new TabPage("About");
            tabControl.TabPages.Add(tabAlbums);
            tabControl.TabPages.Add(tabArtists);
            tabControl.TabPages.Add(tabTracks);
            tabControl.TabPages.Add(tabAbout);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            // About Tab
            Panel aboutPanel = new Panel { Dock = DockStyle.Fill };
            
            RichTextBox aboutText = new RichTextBox
            {
                Location = new Point(10, 10),
                Size = new Size(950, 300),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f)
            };
            aboutText.Text = "Project Cele (working title)\nPreferism Patch 1.5\n\n" +
                            "Made by: fanfare.\n" +
                            "Contact: 88g on flirb.net\n\n" +
                            "Patch 1.5 Notes:\n" +
                            "- Added drag-and-drop support for .mp3 files\n" +
                            "- Added Hard Reset button (clears all app data)\n" +
                            "- Added Hidden Artists manager (view and restore hidden artists)\n" +
                            "- Added artist merge feature (combine collaborations with main artist)\n\n" +
                            "Patch 1 Notes:\n" +
                            "- Fixed info card overlap issues with list views\n" +
                            "- Added Preferism system for Albums (10% boost in shuffle)\n" +
                            "- Added album type classification (Album, Mixtape, EP, Compilation)\n" +
                            "- Added album editing (cover art, artist, type, year)\n" +
                            "- Added artist deletion (hides artist without removing tracks)\n" +
                            "- Replaced emoji symbols with Unicode text\n" +
                            "- Fixed C# 4.0 compatibility for Visual Studio 2010\n" +
                            "- Added full-color album artwork support";
            aboutText.SelectionStart = 0;
            aboutText.SelectionLength = "Project Cele (working title)".Length;
            aboutText.SelectionFont = new Font("Segoe UI", 12f, FontStyle.Bold);

            Button btnHardReset = new Button
            {
                Text = "Hard Reset",
                Location = new Point(10, 320),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnHardReset.Click += BtnHardReset_Click;

            Button btnHiddenArtists = new Button
            {
                Text = "Hidden Artists",
                Location = new Point(140, 320),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnHiddenArtists.Click += BtnHiddenArtists_Click;

            aboutPanel.Controls.Add(aboutText);
            aboutPanel.Controls.Add(btnHardReset);
            aboutPanel.Controls.Add(btnHiddenArtists);
            tabAbout.Controls.Add(aboutPanel);

            // All Tracks ListView
            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            listView.Columns.Add("Title", 260);
            listView.Columns.Add("Artist", 200);
            listView.Columns.Add("Album", 200);
            listView.DoubleClick += ListView_DoubleClick;
            listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
            tabTracks.Controls.Add(listView);

            // Track info panel
            trackInfoPanel = new Panel
            {
                Height = 100,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(240, 245, 250),
                Visible = false
            };
            tabTracks.Controls.Add(trackInfoPanel);
            trackInfoPanel.BringToFront();

            trackCoverPic = new PictureBox
            {
                Size = new Size(80, 80),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            trackInfoPanel.Controls.Add(trackCoverPic);

            lblTrackTitle = new Label { Location = new Point(100, 10), Size = new Size(400, 25), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            lblTrackDuration = new Label { Location = new Point(510, 10), Size = new Size(150, 25) };
            trackInfoPanel.Controls.Add(lblTrackTitle);
            trackInfoPanel.Controls.Add(lblTrackDuration);

            lblTrackArtist = new Label { Location = new Point(100, 40), Size = new Size(400, 25) };
            lblTrackAlbum = new Label { Location = new Point(510, 40), Size = new Size(150, 25) };
            trackInfoPanel.Controls.Add(lblTrackArtist);
            trackInfoPanel.Controls.Add(lblTrackAlbum);

            btnShuffleAll = CreateAeroButton("Shuffle All", 700, 120);
            btnShuffleAll.Location = new Point(700, 25);
            btnShuffleAll.Click += BtnShuffleAll_Click;
            trackInfoPanel.Controls.Add(btnShuffleAll);

            // Artists ListView (icon view)
            listViewArtists = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.LargeIcon,
                FullRowSelect = true
            };
            artistImageList = new ImageList { ImageSize = new Size(64, 64), ColorDepth = ColorDepth.Depth32Bit };
            listViewArtists.LargeImageList = artistImageList;
            listViewArtists.DoubleClick += ListViewArtists_DoubleClick;
            listViewArtists.SelectedIndexChanged += ListViewArtists_SelectedIndexChanged;
            tabArtists.Controls.Add(listViewArtists);

            // Artist info panel
            artistInfoPanel = new Panel
            {
                Height = 100,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(240, 245, 250),
                Visible = false
            };
            tabArtists.Controls.Add(artistInfoPanel);
            artistInfoPanel.BringToFront();

            artistCoverPic = new PictureBox
            {
                Size = new Size(80, 80),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            artistInfoPanel.Controls.Add(artistCoverPic);

            lblArtistName = new Label { Location = new Point(100, 10), Size = new Size(350, 25), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            lblArtistBorn = new Label { Location = new Point(460, 10), Size = new Size(150, 25) };
            artistInfoPanel.Controls.Add(lblArtistName);
            artistInfoPanel.Controls.Add(lblArtistBorn);

            lblArtistReleases = new Label { Location = new Point(100, 40), Size = new Size(150, 25) };
            lblArtistMostPlayed = new Label { Location = new Point(260, 40), Size = new Size(350, 25) };
            artistInfoPanel.Controls.Add(lblArtistReleases);
            artistInfoPanel.Controls.Add(lblArtistMostPlayed);

            btnArtistEdit = CreateAeroButton("Edit", 650, 60);
            btnArtistEdit.Location = new Point(650, 25);
            btnArtistEdit.Click += BtnArtistEdit_Click;

            btnArtistPrefer = CreateAeroButton("Prefer", 720, 100);
            btnArtistPrefer.Location = new Point(720, 25);
            btnArtistPrefer.Click += BtnArtistPrefer_Click;

            btnArtistDelete = CreateAeroButton("Delete", 830, 70);
            btnArtistDelete.Location = new Point(830, 25);
            btnArtistDelete.Click += BtnArtistDelete_Click;

            artistInfoPanel.Controls.Add(btnArtistEdit);
            artistInfoPanel.Controls.Add(btnArtistPrefer);
            artistInfoPanel.Controls.Add(btnArtistDelete);

            // Albums ListView (icon view)
            listViewAlbums = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.LargeIcon,
                FullRowSelect = true
            };
            albumImageList = new ImageList { ImageSize = new Size(64, 64), ColorDepth = ColorDepth.Depth32Bit };
            listViewAlbums.LargeImageList = albumImageList;
            listViewAlbums.DoubleClick += ListViewAlbums_DoubleClick;
            listViewAlbums.SelectedIndexChanged += ListViewAlbums_SelectedIndexChanged;
            tabAlbums.Controls.Add(listViewAlbums);

            // Album info panel
            albumInfoPanel = new Panel
            {
                Height = 100,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(240, 245, 250),
                Visible = false
            };
            tabAlbums.Controls.Add(albumInfoPanel);
            albumInfoPanel.BringToFront();

            albumCoverPic = new PictureBox
            {
                Size = new Size(80, 80),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            albumInfoPanel.Controls.Add(albumCoverPic);

            lblAlbumTitle = new Label { Location = new Point(100, 10), Size = new Size(350, 25), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            lblAlbumDuration = new Label { Location = new Point(460, 10), Size = new Size(100, 25) };
            lblAlbumType = new Label { Location = new Point(570, 10), Size = new Size(100, 25) };
            albumInfoPanel.Controls.Add(lblAlbumTitle);
            albumInfoPanel.Controls.Add(lblAlbumDuration);
            albumInfoPanel.Controls.Add(lblAlbumType);

            lblAlbumArtist = new Label { Location = new Point(100, 40), Size = new Size(350, 25) };
            lblAlbumGenre = new Label { Location = new Point(460, 40), Size = new Size(210, 25) };
            albumInfoPanel.Controls.Add(lblAlbumArtist);
            albumInfoPanel.Controls.Add(lblAlbumGenre);

            btnAlbumPlay = CreateAeroButton("Play", 680, 50);
            btnAlbumPlay.Location = new Point(680, 25);
            btnAlbumPlay.Click += BtnAlbumPlay_Click;

            btnAlbumShuffle = CreateAeroButton("Shuffle", 740, 70);
            btnAlbumShuffle.Location = new Point(740, 25);
            btnAlbumShuffle.Click += BtnAlbumShuffle_Click;

            btnAlbumEdit = CreateAeroButton("Edit", 820, 60);
            btnAlbumEdit.Location = new Point(820, 25);
            btnAlbumEdit.Click += BtnAlbumEdit_Click;

            btnAlbumPrefer = CreateAeroButton("Prefer", 890, 70);
            btnAlbumPrefer.Location = new Point(890, 25);
            btnAlbumPrefer.Click += BtnAlbumPrefer_Click;

            albumInfoPanel.Controls.Add(btnAlbumPlay);
            albumInfoPanel.Controls.Add(btnAlbumShuffle);
            albumInfoPanel.Controls.Add(btnAlbumEdit);
            albumInfoPanel.Controls.Add(btnAlbumPrefer);

            // Album Art & Labels
            picCover = new PictureBox { Location = new Point(10, 420), Size = new Size(120, 120), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            picCover.Paint += PicCover_Paint;

            lblTitle = new Label { Location = new Point(140, 420), Size = new Size(700, 25), Font = new Font(Font, FontStyle.Bold) };
            lblArtistAlbum = new Label { Location = new Point(140, 445), Size = new Size(700, 25) };

            // Bottom gradient panel
            bottomPanel = new Panel { Height = 90, Dock = DockStyle.Bottom };
            bottomPanel.Paint += BottomPanel_Paint;

            // Buttons
            btnPrev = CreateAeroButton("Prev", 20, 42);
            btnPlay = CreateAeroButton("Play", 70, 42);
            btnStop = CreateAeroButton("Stop", 120, 42);
            btnNext = CreateAeroButton("Next", 170, 42);

            btnPrev.Click += BtnPrev_Click;
            btnPlay.Click += BtnPlay_Click;
            btnStop.Click += BtnStop_Click;
            btnNext.Click += BtnNext_Click;

            bottomPanel.Controls.Add(btnPrev);
            bottomPanel.Controls.Add(btnPlay);
            bottomPanel.Controls.Add(btnStop);
            bottomPanel.Controls.Add(btnNext);

            // Progress bar & time
            progressBar = new AeroProgressBar { Location = new Point(240, 35), Size = new Size(500, 18) };
            progressBar.MouseClick += ProgressBar_MouseClick;
            bottomPanel.Controls.Add(progressBar);

            lblTime = new Label { Location = new Point(760, 33), AutoSize = true, Text = "00:00 / 00:00", Font = new Font("Segoe UI", 9f), BackColor = Color.Transparent };
            bottomPanel.Controls.Add(lblTime);

            // Media player
            player = new AxWindowsMediaPlayer();
            ((System.ComponentModel.ISupportInitialize)(player)).BeginInit();
            player.Visible = false;
            player.PlayStateChange += Player_PlayStateChange;
            Controls.Add(player);
            ((System.ComponentModel.ISupportInitialize)(player)).EndInit();

            // Timer
            progressTimer = new Timer { Interval = 500 };
            progressTimer.Tick += ProgressTimer_Tick;
            progressTimer.Start();

            // Add controls to form
            Controls.Add(tabControl);
            Controls.Add(picCover);
            Controls.Add(lblTitle);
            Controls.Add(lblArtistAlbum);
            Controls.Add(bottomPanel);
        }

        private Button CreateAeroButton(string text, int x, int width)
        {
            Button b = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(width, 42),
                Location = new Point(x, 20),
                FlatStyle = FlatStyle.System
            };
            return b;
        }

        private void BottomPanel_Paint(object sender, PaintEventArgs e)
        {
            Rectangle r = bottomPanel.ClientRectangle;
            using (var brush = new LinearGradientBrush(r, Color.FromArgb(210, 225, 245), Color.FromArgb(160, 190, 230), 90f))
            {
                e.Graphics.FillRectangle(brush, r);
            }
            using (Pen p = new Pen(Color.FromArgb(120, 150, 180)))
            {
                e.Graphics.DrawLine(p, 0, 0, r.Width, 0);
            }
        }

        private void PicCover_Paint(object sender, PaintEventArgs e)
        {
            if (picCover.Image == null) return;
            Rectangle r = picCover.ClientRectangle; r.Inflate(-2, -2);
            using (Pen glow = new Pen(Color.FromArgb(120, 170, 220), 2))
                e.Graphics.DrawRectangle(glow, r);
        }

        // ================= DRAG AND DROP =================
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            int added = 0;

            foreach (string file in files)
            {
                if (Path.GetExtension(file).ToLower() == ".mp3")
                {
                    try
                    {
                        var tagFile = TagLib.File.Create(file);
                        Image cover = null;
                        if (tagFile.Tag.Pictures.Length > 0)
                        {
                            byte[] data = tagFile.Tag.Pictures[0].Data.Data;
                            using (MemoryStream ms = new MemoryStream(data))
                                cover = Image.FromStream(ms);
                        }

                        Track newTrack = new Track
                        {
                            FilePath = file,
                            Title = tagFile.Tag.Title ?? Path.GetFileNameWithoutExtension(file),
                            Artist = tagFile.Tag.FirstPerformer ?? "Unknown Artist",
                            Album = tagFile.Tag.Album ?? "Unknown Album",
                            Genre = tagFile.Tag.FirstGenre ?? "Unknown Genre",
                            Year = (int)tagFile.Tag.Year,
                            TrackNumber = (int)tagFile.Tag.Track,
                            Duration = tagFile.Properties.Duration,
                            Cover = cover
                        };

                        // Check if not already in list
                        if (!tracks.Any(t => t.FilePath == file))
                        {
                            tracks.Add(newTrack);
                            added++;
                        }
                    }
                    catch { }
                }
            }

            if (added > 0)
            {
                LoadAllTracks();
                LoadArtists();
                LoadAlbums();
                MessageBox.Show(added.ToString() + " track(s) added!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ================= DATA PERSISTENCE =================
        private void LoadArtistData()
        {
            string dataPath = Path.Combine(Application.StartupPath, "artists.dat");
            if (!System.IO.File.Exists(dataPath)) return;

            try
            {
                foreach (string line in System.IO.File.ReadAllLines(dataPath))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        artistData[parts[0]] = new ArtistInfo
                        {
                            Name = parts[0],
                            Born = parts[1],
                            CustomImage = parts[2],
                            PreferenceBoost = parts.Length > 3 ? int.Parse(parts[3]) : 0
                        };
                    }
                }
            }
            catch { }
        }

        private void SaveArtistData()
        {
            string dataPath = Path.Combine(Application.StartupPath, "artists.dat");
            try
            {
                List<string> lines = new List<string>();
                foreach (var kvp in artistData)
                {
                    lines.Add(string.Format("{0}|{1}|{2}|{3}", kvp.Value.Name, kvp.Value.Born, kvp.Value.CustomImage, kvp.Value.PreferenceBoost));
                }
                System.IO.File.WriteAllLines(dataPath, lines.ToArray());
            }
            catch { }
        }

        private void LoadAlbumData()
        {
            string dataPath = Path.Combine(Application.StartupPath, "albums.dat");
            if (!System.IO.File.Exists(dataPath)) return;

            try
            {
                foreach (string line in System.IO.File.ReadAllLines(dataPath))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 5)
                    {
                        albumData[parts[0]] = new AlbumInfo
                        {
                            Name = parts[0],
                            CustomCover = parts[1],
                            CustomArtist = parts[2],
                            Type = parts[3],
                            Year = parts[4],
                            PreferenceBoost = parts.Length > 5 ? int.Parse(parts[5]) : 0
                        };
                    }
                }
            }
            catch { }
        }

        private void SaveAlbumData()
        {
            string dataPath = Path.Combine(Application.StartupPath, "albums.dat");
            try
            {
                List<string> lines = new List<string>();
                foreach (var kvp in albumData)
                {
                    lines.Add(string.Format("{0}|{1}|{2}|{3}|{4}|{5}", kvp.Value.Name, kvp.Value.CustomCover, kvp.Value.CustomArtist, kvp.Value.Type, kvp.Value.Year, kvp.Value.PreferenceBoost));
                }
                System.IO.File.WriteAllLines(dataPath, lines.ToArray());
            }
            catch { }
        }

        private void LoadHiddenArtists()
        {
            string dataPath = Path.Combine(Application.StartupPath, "hidden_artists.dat");
            if (!System.IO.File.Exists(dataPath)) return;

            try
            {
                hiddenArtists = new List<string>(System.IO.File.ReadAllLines(dataPath));
            }
            catch { }
        }

        private void SaveHiddenArtists()
        {
            string dataPath = Path.Combine(Application.StartupPath, "hidden_artists.dat");
            try
            {
                System.IO.File.WriteAllLines(dataPath, hiddenArtists.ToArray());
            }
            catch { }
        }

        private void LoadArtistMerges()
        {
            string dataPath = Path.Combine(Application.StartupPath, "artist_merges.dat");
            if (!System.IO.File.Exists(dataPath)) return;

            try
            {
                foreach (string line in System.IO.File.ReadAllLines(dataPath))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length == 2)
                    {
                        artistMerges[parts[0]] = parts[1];
                    }
                }
            }
            catch { }
        }

        private void SaveArtistMerges()
        {
            string dataPath = Path.Combine(Application.StartupPath, "artist_merges.dat");
            try
            {
                List<string> lines = new List<string>();
                foreach (var kvp in artistMerges)
                {
                    lines.Add(string.Format("{0}|{1}", kvp.Key, kvp.Value));
                }
                System.IO.File.WriteAllLines(dataPath, lines.ToArray());
            }
            catch { }
        }

        private string GetMergedArtist(string artist)
        {
            if (artistMerges.ContainsKey(artist))
                return artistMerges[artist];
            return artist;
        }

        private ArtistInfo GetArtistInfo(string artist)
        {
            if (!artistData.ContainsKey(artist))
            {
                artistData[artist] = new ArtistInfo { Name = artist, Born = "", CustomImage = "", PreferenceBoost = 0 };
            }
            return artistData[artist];
        }

        private AlbumInfo GetAlbumInfo(string album)
        {
            if (!albumData.ContainsKey(album))
            {
                albumData[album] = new AlbumInfo { Name = album, CustomCover = "", CustomArtist = "", Type = "Album", Year = "", PreferenceBoost = 0 };
            }
            return albumData[album];
        }

        // ================= ABOUT BUTTONS =================
        private void BtnHardReset_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "WARNING: This will delete ALL app data including:\n\n" +
                "- Artist preferences and metadata\n" +
                "- Album preferences and metadata\n" +
                "- Hidden artists list\n" +
                "- Artist merges\n\n" +
                "Your music files will NOT be deleted.\n\n" +
                "Are you absolutely sure?",
                "Hard Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    string[] dataFiles = {
                        "artists.dat",
                        "albums.dat",
                        "hidden_artists.dat",
                        "artist_merges.dat"
                    };

                    foreach (string file in dataFiles)
                    {
                        string path = Path.Combine(Application.StartupPath, file);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }

                    artistData.Clear();
                    albumData.Clear();
                    hiddenArtists.Clear();
                    artistMerges.Clear();

                    LoadAllTracks();
                    LoadArtists();
                    LoadAlbums();

                    MessageBox.Show("All app data has been reset.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error during reset: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnHiddenArtists_Click(object sender, EventArgs e)
        {
            Form hiddenForm = new Form
            {
                Text = "Hidden Artists",
                Width = 400,
                Height = 400,
                FormBorderStyle = FormBorderStyle.Sizable,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false
            };

            ListView lstHidden = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true
            };
            lstHidden.Columns.Add("Artist", 250);

            foreach (string artist in hiddenArtists)
            {
                ListViewItem item = new ListViewItem(artist);
                item.Tag = artist;
                lstHidden.Items.Add(item);
            }

            Panel btnPanel = new Panel { Height = 50, Dock = DockStyle.Bottom };
            Button btnRestore = new Button
            {
                Text = "Restore Selected",