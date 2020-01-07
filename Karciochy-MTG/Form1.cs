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

namespace Karciochy_MTG
{
    public partial class MainForm : Form
    {
        private CardService cardService;
        private List<List<Card>> cardPages;
        private string setName;
        public MainForm()
        {
            InitializeComponent();


            dataGridView1.RowTemplate.Height = 150;

            cardService = new CardService();


            //var sets = setService.
            cardPages = new List<List<Card>>();





            //var ff = GetSet();
            //GetCardsButton.Enabled = false;

            GetSet();






        }

        void GetSet()
        {
            var setService = new SetService();
            var s =  setService.All();
            if (s.IsSuccess)
            {
                var exp = s.Value.Select(d=>d.Name).ToList<string>();
                SetComboBox.Items.AddRange(exp.ToArray<string>());
                SetComboBox.SelectedItem = SetComboBox.Items[0];
            }
            else throw new Exception("FAIL");

        }

        async Task SetupCards(string CardName, string CardSet)
        {
            cardPages.Clear();
            var cardCollection = cardService.Where(z => z.SetName, CardSet).Where(x => x.Rarity, "Common").Where(c => c.Name, CardName);
            var getCollection = await cardCollection.AllAsync();
            int pagesCount = getCollection.PagingInfo.TotalPages;

            for (int i = 0; i <= pagesCount; i++)
            {
                var page = cardCollection.Where(p => p.Page, i + 1).All();
                if (page.IsSuccess)
                {
                    cardPages.Add(page.Value);
                }

            }
            
        }

        public async Task<MtgApiManager.Lib.Core.Exceptional<Card>> GetCard()
        {
            var result = cardService.Find("f2eb06047a3a8e515bff62b55f29468fcde6332a");
            var asyncResult = await cardService.FindAsync("f2eb06047a3a8e515bff62b55f29468fcde6332a");
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
            dataGridView1.Rows.Clear();
            Task.Run(SetCards);




        }

        async public Task SetCards()
        {
            await SetupCards(cardNameTextBox.Text, setName);

            foreach (var cardPage in cardPages)
            {
                 foreach (var card in cardPage)
                 {
                     if (card.ImageUrl != null)
                     {
                        _ = Task.Run(() =>
                          {
                              Image img = DownloadImage(card.ImageUrl.AbsoluteUri);

                              if (dataGridView1.InvokeRequired)
                              {
                                  dataGridView1.Invoke(new MethodInvoker(() =>
                                  {
                                      dataGridView1.Rows.Add(new object[] { dataGridView1.Rows.Count, card.Name, card.SetName, card.Text, card.ImageUrl, img });
                                      dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0];
                                    //dataGridView1.Refresh();
                                  }));

                              }
                              else
                              {
                                  dataGridView1.Rows.Add(new object[] { dataGridView1.Rows.Count, card.Name, card.SetName, card.Text, card.ImageUrl, img });
                                  dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0];
                                //dataGridView1.Refresh();
                              }
                          });
                             
                         



                     }
                 }
            }

             if (GetCardsButton.InvokeRequired)
                 GetCardsButton.Invoke(new MethodInvoker(() => {
                     GetCardsButton.Enabled = true;
                 }));
             else GetCardsButton.Enabled = true;

             


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
