using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WatchedAnimeList.ViewModels;

namespace WatchedAnimeList.Controls
{
    public partial class PlayerWithButtons : UserControl
    {
        public readonly MpvPlayer player;
        private Button btnPause;

        public PlayerWithButtons()
        {
            InitializeComponent();

            player = new MpvPlayer();
            player.Dock = DockStyle.Fill;
            this.Controls.Add(player);

            btnPause = new Button();
            btnPause.Text = "Pause";
            btnPause.Dock = DockStyle.Bottom;
            btnPause.Click += BtnPause_Click;
            this.Controls.Add(btnPause);
        }
        bool pause = false;

        public void BtnPause_Click(object sender, EventArgs agr)
        {
            if (pause)
            {
                pause = !pause;
                player.ExecuteCommand("set pause no");   // Відтворити
            }
            else
            {
                pause = !pause;
                player.ExecuteCommand("set pause yes");  // Пауза
            }
        }

        public void Load(string path)
        {
            player.Load(path);
        }
    }

}
