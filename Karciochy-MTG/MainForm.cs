using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MtgApiManager.Lib.Core;
using MtgApiManager.Lib.Model;
using MtgApiManager.Lib.Service;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Karciochy_MTG
{
    public partial class MainForm : Form
    {
        private CardService cardService;
        private List<List<Card>> cardPages;
        private string setName, cardRarity;
        public string[] Rarity = new string[] { "Basic Land", "Common", "Uncommon", "Rare", "Mythic Rare", "Special" };
        public MainForm()
        {
            InitializeComponent();

            dataGridView1.RowTemplate.Height = 100;

            cardService = new CardService();
            cardPages = new List<List<Card>>();
            rarityComboBox.Items.AddRange(Rarity);
            SetComboBox.Enabled = false;

            Task.Run(GetSet);
            //ScryfallCard();
            progressBar1.Value = 0;
        }

        public void SetWinFormControll(Control winFormControl, MethodInvoker method)
        {
            if(winFormControl.InvokeRequired)
            {
                winFormControl.Invoke(method);
            }
            else
            {
                method.Invoke();
            }
        }

        async Task GetSet()
        {
            var setService = new SetService();
            
            var s =  await setService.AllAsync();
            if (s.IsSuccess)
            {
                var exp = s.Value.Select(d=>d.Name).ToList<string>();

                SetWinFormControll(SetComboBox, () => 
                {
                    progressBar1.Style = ProgressBarStyle.Blocks;
                    SetComboBox.Enabled = true;
                    SetComboBox.Items.AddRange(exp.ToArray<string>());
                    SetComboBox.SelectedItem = SetComboBox.Items[0];
                });
               
            }
            else throw new Exception("FAIL");

        }

        async Task SetupCards(string CardName, string CardSet, string Rarity)
        {
            cardPages.Clear();

            SetWinFormControll(progressBar1, new MethodInvoker(() => progressBar1.Style = ProgressBarStyle.Marquee));

            var cardCollection = cardService.Where(z => z.SetName, CardSet).Where(r=>r.Rarity, Rarity).Where(c => c.Name, CardName);
            var getCollection = await cardCollection.AllAsync();
            int pagesCount = getCollection.PagingInfo.TotalPages;
            bool pagesCompleted = false;
            //await Task.Run(() =>
            //{
                for (int i = 0; i <= pagesCount; i++)
                {
                    //pageTsks.Add(Task.Run(() =>
                    //{
                        var page = await cardCollection.Where(p => p.Page, i + 1).AllAsync();
                        if (page.IsSuccess)
                        {
                            cardPages.Add(page.Value);
                        }
                        else
                        {
                    throw page.Exception;
                        }
                    //}));

                }


            //do
            //{
            //    pagesCompleted = pageTsks.Where(t => t.Status == TaskStatus.RanToCompletion).Count() == pagesCount;
            //} while (!pagesCompleted);
            SetWinFormControll(progressBar1, new MethodInvoker(() => progressBar1.Style = ProgressBarStyle.Blocks));

            Console.WriteLine("PAGES COMPLETED");
           // });
            
        }

        public async Task<MtgApiManager.Lib.Core.Exceptional<Card>> GetCard()
        {
            var asyncResult = await cardService.FindAsync("03f4341c088b4f35b82b3d98d8a93de4");
            return asyncResult;

        }

        async public Task<Image> GetImage(string fromUrl)
        {
            using (System.Net.WebClient webClient = new System.Net.WebClient())
            {
                using (Stream stream = await webClient.OpenReadTaskAsync(fromUrl))
                {
                    return System.Drawing.Image.FromStream(stream);
                }
            }
        }

        public System.Drawing.Image DownloadImage(string imageUrl)
        {
            System.Drawing.Image image = null;

            try
            {
                System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
                webRequest.AllowWriteStreamBuffering = true;
                webRequest.Timeout = 30000;
                webRequest.ServicePoint.ConnectionLeaseTimeout = 5000;
                webRequest.ServicePoint.MaxIdleTime = 5000;

                using (System.Net.WebResponse webResponse = webRequest.GetResponse())
                {

                    using (System.IO.Stream stream = webResponse.GetResponseStream())
                    {
                        image = System.Drawing.Image.FromStream(stream);
                    }
                }

                webRequest.ServicePoint.CloseConnectionGroup(webRequest.ConnectionGroupName);
                webRequest = null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);

            }
            return image;
        }

        private void GetCardsButton_Click(object sender, EventArgs e)
        {
            GetCardsButton.Enabled = false;
            setName = SetComboBox.Text;
            cardRarity = rarityComboBox.Text;
            dataGridView1.Rows.Clear();
            Task.Run(SetCards);
        }

        public async Task<Image> ScryfallCard(int multiverseId)
         {
            string baseUrl = "https://api.scryfall.com/";
            string quality = "normal"; // large small normal
            string cardUrl = string.Format("cards/multiverse/{0}?format=image&amp;version={1}", multiverseId, quality);

            var uri = new Uri(Path.Combine(baseUrl, cardUrl));

            using (System.Net.WebClient webClient = new System.Net.WebClient())
            {

                using (Stream stream = await webClient.OpenReadTaskAsync(uri))
                {
                    Image cardImage = System.Drawing.Image.FromStream(stream);
                    return cardImage;
                }
            }

         }

        async public Task SetCards()
        {
            await SetupCards(cardNameTextBox.Text, setName, cardRarity);

            int totalCardCount = cardPages.Select(s=>s.Where(u=>u.ImageUrl != null).Count()).Sum();

            foreach (var cardPage in cardPages)
            {
                 foreach (var card in cardPage)
                 {
                     if (card.ImageUrl != null)
                     {
                         SetCard(card, totalCardCount);
                     }
                 }
            }

            SetWinFormControll(GetCardsButton, () => GetCardsButton.Enabled = true);
             
        }
         private async Task SetCard(Card card, int totalCardCount)
        {
            Image img = await ScryfallCard(card.MultiverseId.Value);//DownloadImage(card.ImageUrl.AbsoluteUri);

            SetWinFormControll(dataGridView1, () =>
            {
                dataGridView1.Rows.Add(new object[] { dataGridView1.Rows.Count, card.Name, card.SetName, card.Text, img });
                dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0];
            });

            SetWinFormControll(progressBar1, () =>
            {
                progressBar1.Value = (int)((float)((float)dataGridView1.Rows.Count / (float)totalCardCount) * 100f);
            });
            Console.WriteLine((int)((float)((float)dataGridView1.Rows.Count / (float)totalCardCount) * 100f));
        }
        private void DataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            pictureBox1.Image = (Image)dataGridView1.Rows[rowIndex].Cells[4].Value;
        }

        //private async Task DownloadImageAsync(dataGridViv Uri uri)
        //{
        //    var httpClient = new HttpClient();

        //    // Get the file extension
        //    var uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
        //    var fileExtension = Path.GetExtension(uriWithoutQuery);

        //    // Create file path and ensure directory exists
        //    var path = Path.Combine(directoryPath, $"{fileName}{fileExtension}");
        //    Directory.CreateDirectory(directoryPath);

        //    // Download the image and write to the file
        //    var imageBytes = await httpClient.GetStreamAsync(uri);
        //}


    }
}
