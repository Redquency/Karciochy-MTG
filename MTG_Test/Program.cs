using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MtgApiManager.Lib.Core;
using MtgApiManager.Lib.Model;
using MtgApiManager.Lib.Service;


namespace MTG_Test
{
    class Program
    {
        
        static void Main(string[] args)
        {
            int counter = 0;
            CardService cardService = new CardService();
            var cardRequest = cardService.Where(x => x.Set, "4ED").Where(x=>x.Type, "Basic Land");
            Exceptional<List<Card>> result = cardRequest.All();

            string setCode = "4ED";
         
            int pagesCount = result.PagingInfo.TotalPages;
            var cardCount = result.PagingInfo.TotalCount;
            Console.WriteLine("card count:" + cardCount);
            string tempName = "";
            int landCounter = 1;
            for (int i = 0; i <= pagesCount; i++)
            {
                var page = cardRequest.Where(p => p.Page, i+1).All();
                if (page.IsSuccess)
                {
                    foreach (Card card in page.Value)
                    {
                        //if(tempName == card.Name)
                        //{
                        //    string name = string.Format("{0}{1}.full.jpg", card.Name, landCounter++);
                        //    Console.WriteLine(++counter + " " + name);
                        //}
                        //else
                        //{
                        //    landCounter = 1;
                        //    Console.WriteLine(++counter + " " + card.Number);

                        //}

                        Console.WriteLine(++counter + " " + card.);


                        tempName = card.Name;
                    }
                }
                else
                {
                    throw page.Exception;
                }
                
            }

            Console.WriteLine("END");
            Console.ReadKey();
        }
    }
}
