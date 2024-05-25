using Microsoft.AspNetCore.Mvc;
using MSIT158Site.Models;
using Newtonsoft.Json;

namespace MSIT158Site.Controllers
{
    public class ApiController : Controller
    {
     private readonly MyDBContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        public ApiController(MyDBContext context,IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
            _context = context;
        }

        public IActionResult CheckName(string name) {
            var check = _context.Members.Any(a => a.Name == name);
            if (check)
            {
                return Content(check.ToString(),"text/plain", System.Text.Encoding.UTF8);
            }
            return Content(check.ToString(), "text/plain", System.Text.Encoding.UTF8);
        }

        public IActionResult Index()
        {
            System.Threading.Thread.Sleep(6000);
            return Content("世界, 您好!!","text/html", System.Text.Encoding.UTF8);
        }

        public IActionResult Cities()
        {
            var cities = _context.Addresses.Select(a => a.City).Distinct();
            return Json(cities);
        }

        public IActionResult Townships(string city)
        {
             
            var townships = _context.Addresses.Where(a => a.City == city)
                .Select(b=>b.SiteId).Distinct();
            return Json(townships);
        }

        public IActionResult Roads(string townships)
        {

            var roads = _context.Addresses.Where(c=>c.SiteId==townships)
                .Select(b=>b.Road).Distinct();
            return Json(roads);
              
        }


        public IActionResult Register(Member member, IFormFile avatar)
        {
            if (string.IsNullOrEmpty(member.Name))
            {
                member.Name = "guest";
            }

            //string info = $"{avatar.FileName} - {avatar.Length} - {avatar.ContentType}";
            //string info = _hostEnvironment.ContentRootPath;

            //檔案上傳
            //實際路徑 詳細看課本p39 主要是功能是動態取得路徑
            //string uploadPath = @"C:\Users\User\Documents\workspace\MSIT158Site\wwwroot\uploads\a.png";
            //WebRootPath：C: \Users\User\Documents\workspace\MSIT158Site\wwwroot
            //ContentRootPath：C:\Users\User\Documents\workspace\MSIT158Site
            string uploadPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", avatar.FileName);
            string info = uploadPath;
            using (var fileStream = new FileStream(uploadPath, FileMode.Create))
            {
                //將檔案copy到路徑
                avatar.CopyTo(fileStream);
            }


            //將圖片存成二進制
            byte[] imgByte = null;
            using (var memoryStream = new MemoryStream()) 
            {
                avatar.CopyTo(memoryStream);
                imgByte = memoryStream.ToArray();  
            }

             
            member.FileName = avatar.FileName;
            member.FileData = imgByte;
            _context.Members.Add(member);
            _context.SaveChanges();

            return Content(info, "text/plain", System.Text.Encoding.UTF8);
            //寫進資料庫

            //我寫的
            //string filePath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", avatar.FileName);
            //using (var fileStream = new FileStream(filePath,FileMode.Create))
            //{
            //    avatar.CopyTo(fileStream);

            //}

            //    string info = $"{avatar.FileName}-{avatar.Length}-{avatar.ContentType}";
            ////return Content($"Hello {member.Name}，{member.Age} 歲了，電子郵件是 {member.Email}", "text/html", System.Text.Encoding.UTF8);
            //return Content(info , "text/plain", System.Text.Encoding.UTF8);

        }
        public IActionResult Avatar(int id =1)
        {
            Member? member = _context.Members.Find(id);
            if(member != null)
            {
                byte[] img = member.FileData;
                if(img != null)
                {
                    return File(img, "image/jpeg");
                }

            }
            return NotFound();
        }

        [HttpPost]
        public IActionResult Spots([FromBody] SearchDTO searchDTO)
        {
            //根據分類編號搜尋景點資料
            var spots = searchDTO.categoryId == 0 ? _context.SpotImagesSpots : _context.SpotImagesSpots.Where(s => s.CategoryId == searchDTO.categoryId);

            //根據關鍵字搜尋景點資料(title、desc)
            if (!string.IsNullOrEmpty(searchDTO.keyword))
            {
                spots = spots.Where(s => s.SpotTitle.Contains(searchDTO.keyword) || s.SpotDescription.Contains(searchDTO.keyword));
            }




            //總共有多少筆資料
            int totalCount = spots.Count();
            //每頁要顯示幾筆資料
            int pageSize = searchDTO.pageSize;   //searchDTO.pageSize ?? 9;
            //目前第幾頁
            int page = searchDTO.page;

            //計算總共有幾頁
            int totalPages = (int)Math.Ceiling((decimal)totalCount / pageSize);

            //分頁
            spots = spots.Skip((page - 1) * pageSize).Take(pageSize);


            //包裝要傳給client端的資料
            SpotsPagingDTO spotsPaging = new SpotsPagingDTO();
            spotsPaging.TotalCount = totalCount;
            spotsPaging.TotalPages = totalPages;
            spotsPaging.SpotsResult = spots.ToList();

            return Json(spotsPaging);
        }


    }
}
