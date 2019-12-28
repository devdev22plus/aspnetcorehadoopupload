using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace aspcorehadoopupload.Controllers
{
    public class HomeController : Controller
    {
        readonly string m_HadoopURL = "";
		readonly int m_HadoopRetryUpload = 0;
        readonly List<KeyValuePair<string, string>> m_URLSwitch = null;

        public HomeController(IHostingEnvironment hostingEnvironment
                            , IConfiguration configuration
                            , IMemoryCache cache
                            )
        {
            m_HadoopURL = configuration["Hadoop:URL"].ToString();
			m_HadoopRetryUpload = Convert.ToInt32(configuration["Hadoop:RETRYUPLOAD"]);
			m_URLSwitch = new List<KeyValuePair<string, string>>();
			List<KeyValuePair<string, string>> ConfigURLSwitchFind = configuration.GetSection("Hadoop:URL_SWITCH:FIND").AsEnumerable().ToList();
			List<KeyValuePair<string, string>> ConfigURLSwitchReplace = configuration.GetSection("Hadoop:URL_SWITCH:REPLACE").AsEnumerable().ToList();
			if (ConfigURLSwitchFind.Count != ConfigURLSwitchReplace.Count) Console.WriteLine("size error of ConfigURLSwitchFind != ConfigURLSwitchReplace");
			else
				for(int ind = 0 ; ind < ConfigURLSwitchFind.Count(); ++ind)
				{
					var find = ConfigURLSwitchFind[ind];
					var replace = ConfigURLSwitchReplace[ind];
					if (find.Value == null || replace.Value == null
						|| find.Value == string.Empty || replace.Value == string.Empty)
						continue;

					m_URLSwitch.Add(new KeyValuePair<string, string>(find.Value, replace.Value));
				}
        }

        public IActionResult Index()
        {
            return View();
        }

        [Route("GetFolderList"), HttpPost]
        public async Task<IActionResult> GetFolderList(string path = "/")
        {
            HDFSResponse.FileStatus[] fileStatuses = await HadoopAPI.GetFileList(m_HadoopURL, path);
            return Content(JsonConvert.SerializeObject(fileStatuses));
        }

        [Route("CreateNewFolder"), HttpPost]
        public async Task<IActionResult> CreateNewFolder(string folderName)
        {
            bool result = await HadoopAPI.CreateFile(m_HadoopURL, folderName);
            return Content(JsonConvert.SerializeObject(new { result = result, }));
        }

        [Route("DeleteFile"), HttpPost]
        public async Task<IActionResult> DeleteFile(string path)
        {
            bool result = await HadoopAPI.DeleteFile(m_HadoopURL, path);
            return Content(JsonConvert.SerializeObject(new { result = result, }));
        }

		//[RequestSizeLimit(2000000000)]
		//[RequestFormLimits(ValueLengthLimit = 2000000000, MultipartBodyLengthLimit = 2000000000)]
		[DisableRequestSizeLimit]
		[RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
		[Route("UploadVideo"), HttpPost]
		public async Task<IActionResult> UploadVideo(string path, IFormFile fileUpload)
        {
            string failedType = string.Empty;
			
			if ( fileUpload == null )
			{
				failedType = "fileUpload is null";
				goto __FAILED;
			}

            string tempFile = Path.GetTempFileName();
			using (FileStream fileStream = System.IO.File.OpenWrite(tempFile))
			{
				fileUpload.CopyTo(fileStream);
			}
            
            // Console.WriteLine("path : " + path);
            // Console.WriteLine("fileUpload.Name : " + fileUpload.Name);


            if ( path != "/" )
            {
                path += "/";
            }

            if (path[0] == '/') path = path.Remove(0, 1);
            
            
            //Console.WriteLine("after path : " + path);
            

            string fileName = Path.GetFileName(fileUpload.FileName);
            string targetPath = path + fileName.MakeValidFileName()
                                                .Replace(" ", "%20")
                                                .Replace("(", "%28")
                                                .Replace(")", "%29")
                                                .Replace("[", "%20")
                                                .Replace("]", "%20")
                                                ;

            byte[] buffer = System.IO.File.ReadAllBytes(tempFile);
            System.IO.File.Delete(tempFile);
            Console.WriteLine("Hadoop URL Upload : " + targetPath);
            bool upload = await HadoopAPI.UploadFile(m_HadoopURL, targetPath, m_URLSwitch, buffer);
            if (!upload)
            {
                failedType = "failed upload to hadoop";
				goto __FAILED;
            }

            return Content(JsonConvert.SerializeObject(new { result = "OK", }));
__FAILED:
            return Content(JsonConvert.SerializeObject(new { result = "FAILED", failedType = failedType, }));
        }
    }
}
