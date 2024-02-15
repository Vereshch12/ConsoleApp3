using AngleSharp;
using System.Text;

namespace WebScraper
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var address = "https://www.invitro.ru/analizes/for-doctors/piter/140/";
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(address);
            var analysisItems = document.QuerySelectorAll(".analyzes-item");

            var absoluteLinks = new List<string>();

            foreach (var item in analysisItems)
            {
                var linkElement = item.QuerySelector(".analyzes-item__title a");
                var link = linkElement != null ? linkElement.GetAttribute("href") : "N/A";
                var resultLink = "https://www.invitro.ru" + link;

                absoluteLinks.Add(resultLink);
            }

            using (var writer = new StreamWriter("analysis_data.csv", false, Encoding.UTF8))
            {
                writer.WriteLine("Артикул,Хлебные крошки,Название анализа,Срок исполнения,Цена анализа,Ссылка на анализ,Название региона");

                var semaphore = new SemaphoreSlim(1, 1);

                var tasks = new List<Task>();
                
                foreach (var link in absoluteLinks)
                {
                    tasks.Add(ProcessAnalysisAsync(link, writer, context, semaphore));
                }

                await Task.WhenAll(tasks);
            }
        }

        static async Task ProcessAnalysisAsync(string link, StreamWriter writer, IBrowsingContext context, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();

            try
            {
                var random = new Random();
                var delay = random.Next(100, 500);
                await Task.Delay(delay);

                var analysisDocument = await context.OpenAsync(link);
                
                var articleElement = analysisDocument.QuerySelector(".info-block__section--article .info-block__price");
                var article = articleElement != null ? articleElement.TextContent.Trim() : "N/A";

                var analysisNameElement = analysisDocument.QuerySelector("#titlePage h1");
                var analysisName = analysisNameElement != null ? analysisNameElement.TextContent.Trim() : "N/A";

                var turnaroundTimeElement = analysisDocument.QuerySelector(".info-block__section--date .info-block__title + .radio .radio__text");
                var turnaroundTime = turnaroundTimeElement != null ? turnaroundTimeElement.TextContent.Trim().Split("?")[0].Trim() : "N/A";

                var priceElement = analysisDocument.QuerySelector(".info-block__section--price .info-block__price");
                var price = priceElement != null ? priceElement.TextContent.Trim() : "N/A";

                /*КОММЕНТАРИЙ: тут пытался сделать так, чтобы "Хлебные крошки" отображались без сокращений, но не хватило времени
                var breadcrumbsItems = analysisDocument.QuerySelectorAll(".bread-crumbs__item span[itemprop='name']");
                var breadcrumbsBuilder = new StringBuilder();
                foreach (var breadcrumbItem in breadcrumbsItems)
                {
                    var breadcrumbText = breadcrumbItem.TextContent.Trim();
                    breadcrumbsBuilder.Append(breadcrumbText + "/");
                }
                var firstPartBreadcrumbs = breadcrumbsBuilder.ToString().TrimEnd('/');
                

                breadcrumbsItems = analysisDocument.QuerySelectorAll(".bread-crumbs__item[origin_text]");
                breadcrumbsBuilder = new StringBuilder();
                foreach (var breadcrumbItem in breadcrumbsItems)
                {
                    var breadcrumbText = breadcrumbItem.GetAttribute("origin_text").Trim();
                    breadcrumbsBuilder.Append(breadcrumbText + "/");
                }
                var secondPartBreadcrumbs = breadcrumbsBuilder.ToString().TrimEnd('/');
                
                var breadcrumbs = $"{firstPartBreadcrumbs}/{secondPartBreadcrumbs}";*/
                
                var breadcrumbsItems = analysisDocument.QuerySelectorAll(".bread-crumbs__item span[itemprop='name']");
                var breadcrumbsBuilder = new StringBuilder("");
                foreach (var breadcrumbItem in breadcrumbsItems)
                {
                    var breadcrumbText = breadcrumbItem.TextContent.Trim();
                    breadcrumbsBuilder.Append(breadcrumbText + "/");
                }

                breadcrumbsBuilder.Append(analysisName);

                var breadcrumbs = breadcrumbsBuilder.ToString();

                var regionElement = analysisDocument.QuerySelector(".city__name.city__btn.city__name--label");
                var region = regionElement != null ? regionElement.TextContent.Trim() : "N/A";

                lock (writer)
                {
                    writer.WriteLine($"{article},{breadcrumbs},{analysisName},{turnaroundTime},{price},{link},{region}");
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
