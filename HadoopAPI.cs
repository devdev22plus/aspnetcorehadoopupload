using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class HadoopAPI
{
    public static async Task CallHadoop(string method, string url, Action<HttpRequestMessage> requestCallback, Action<HttpResponseMessage> responseCallback)
    {
        using(HttpClientHandler httpClientHandler = new HttpClientHandler())
        {
            httpClientHandler.AllowAutoRedirect = false;

            using (var httpClient = new HttpClient(httpClientHandler))
            {
#if DEBUG
                Console.WriteLine("CallHadoop URL : " + url);
#endif
                using (var request = new HttpRequestMessage(new HttpMethod(method), url))
                {
                    if (requestCallback != null) requestCallback(request);
                    //request.Content = new ByteArrayContent(File.ReadAllBytes("text.txt"));

                    var response = await httpClient.SendAsync(request);


#if DEBUG
                    // Console.WriteLine("Request:");
                    // Console.WriteLine(request.ToString());
                    // Console.WriteLine("============================");

                    // Console.WriteLine("Response:");
                    // Console.WriteLine(response.ToString());
                    // Console.WriteLine("============================");

                    // Console.WriteLine("Response-Content:");
                    // Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                    // Console.WriteLine("============================");
#endif


                    responseCallback(response);

                }
            }
        }

        await Task.Yield();
    }

    public static async Task<T> CallHadoop<T>(string method, string url, Action<HttpRequestMessage> requestCallback, HttpStatusCode responseCode) where T : class
    {
        T ret = null;

        await CallHadoop(method, url, requestCallback, response => {
            if (response.StatusCode == responseCode)
            {
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                ret = JsonConvert.DeserializeObject<T>(jsonResponse);
            }
        });

        //await Task.Yield();
        return ret;
    }

    //Upload
    public static async Task<bool> UploadFile(string hadoopURL, string filePath, IEnumerable<KeyValuePair<string, string>> URLSwitch, byte[] datas)
    {
        HDFSResponse.Redirection createFile = await HadoopAPI.CallHadoop<HDFSResponse.Redirection>("PUT", $"{hadoopURL}webhdfs/v1/{filePath}?op=CREATE&overwrite=true&noredirect=true", null, HttpStatusCode.OK);
        if (createFile != null)
        {
            if (URLSwitch != null) createFile.URLSwitch(URLSwitch);

            string responseContent = "";

            HttpStatusCode responseHttpStatusCode = HttpStatusCode.NotFound;
            await CallHadoop("PUT", createFile.Location, request => {
                request.Content = (datas != null) ? new ByteArrayContent(datas) : null;
            }, response => {
                responseHttpStatusCode = response.StatusCode;
                responseContent = response.Content.ReadAsStringAsync().Result;
            });

#if DEBUG
            Console.WriteLine("responseHttpStatusCode : " + responseHttpStatusCode);
#endif

            switch(responseHttpStatusCode)
            {
                //OK
                case HttpStatusCode.Created: return true;

                //case HttpStatusCode.Forbidden:
                default:
                {
#if DEBUG
                    Console.WriteLine("Debug Response Content");
                    Console.WriteLine(responseContent);
#endif
                }
                break;
            }
        }

        return false;
    }
    
    public static async Task<bool> UploadFileAppend(string hadoopURL, string filePath, IEnumerable<KeyValuePair<string, string>> URLSwitch, byte[] datas)
    {
        string dataNodeUrl = "";

        await HadoopAPI.CallHadoop("POST", $"{hadoopURL}webhdfs/v1/{filePath}?op=APPEND", x => {
        }, x => {
            if ( x.StatusCode == HttpStatusCode.TemporaryRedirect )
            {
                dataNodeUrl = x.Headers.Location.ToString();
            }
        });

        if (dataNodeUrl != "")
        {
            if(URLSwitch != null)
            {
                HDFSResponse.Redirection createFile = new HDFSResponse.Redirection(){ Location = dataNodeUrl, };
                createFile.URLSwitch(URLSwitch);

                dataNodeUrl = createFile.Location;
            }

            //Console.WriteLine(dataNodeUrl);

            bool OK = false;

            await HadoopAPI.CallHadoop("POST", dataNodeUrl, request => {
                request.Content = (datas != null) ? new ByteArrayContent(datas) : null;
            }, x => {
                OK = x.StatusCode == HttpStatusCode.OK;
            });

            return OK;
        }
        

        return false;
    }

    //Delete
    public static async Task<bool> CreateFile(string hadoopURL, string filePath, string permission = "777")
    {
        bool ret = false;
        HttpStatusCode responseHttpStatusCode = HttpStatusCode.NotFound;
        await CallHadoop("PUT", $"{hadoopURL}webhdfs/v1/{filePath}?op=MKDIRS&permission={permission}", null, response => {
            responseHttpStatusCode = response.StatusCode;
            if (responseHttpStatusCode == HttpStatusCode.OK)
            {
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                //Console.WriteLine(jsonResponse);

                dynamic jsonData = JsonConvert.DeserializeObject(jsonResponse);
                ret = jsonData.boolean;
            }
        });

        return ret;
    }

    //Delete
    public static async Task<bool> DeleteFile(string hadoopURL, string filePath)
    {
        HDFSResponse.DeleteFile deleteFile = null;
        HttpStatusCode responseHttpStatusCode = HttpStatusCode.NotFound;
        await CallHadoop("DELETE", $"{hadoopURL}webhdfs/v1/{filePath}?op=DELETE&recursive=true", null, response => {
            responseHttpStatusCode = response.StatusCode;
            if (responseHttpStatusCode == HttpStatusCode.OK)
            {
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                deleteFile = JsonConvert.DeserializeObject<HDFSResponse.DeleteFile>(jsonResponse);
            }
        });

        return deleteFile.boolean;
    }

    //Read
    public static async Task<byte[]> OpenRead(string hadoopURL, string filePath, IEnumerable<KeyValuePair<string, string>> URLSwitch)
    {
        byte[] byteRet = null;

        HDFSResponse.Redirection createFile = await HadoopAPI.CallHadoop<HDFSResponse.Redirection>("GET", $"{hadoopURL}webhdfs/v1/{filePath}?op=OPEN&noredirect=true", null, HttpStatusCode.OK);
        if (createFile != null)
        {
            if(URLSwitch != null) createFile.URLSwitch(URLSwitch);

            await CallHadoop("GET", createFile.Location, null, response => {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using(Stream stream = response.Content.ReadAsStreamAsync().Result)
                    {
                        //using(MemoryStream memoryStream = new MemoryStream())
                        {
                            //stream.CopyToAsync(memoryStream);

                            using (BinaryReader reader = new BinaryReader(stream))
                            {
                                //limit 4GB
                                //แต่คิดว่าไม่น่าจะมีปัญหาเนื่องจากเราไม่ได้ใช้ในการอ่านไฟล์ขนาดใหญ่อยู่แล้ว
                                byteRet = reader.ReadBytes((int)stream.Length);

                                // Console.WriteLine("");
                                // for(int i = 0 ; i < stream.Length ; ++i)
                                // {
                                //     Console.Write(reader.ReadByte().ToString("X2"));
                                // }
                            }
                        }
                    }
                }
            });
        }

        return byteRet;
    }

    public static async Task<HDFSResponse.FileStatus[]> GetFileList(string hadoopURL, string filePath)
    {
        HDFSResponse.FileStatus[] files = null;

        HttpStatusCode responseHttpStatusCode = HttpStatusCode.NotFound;
        await CallHadoop("GET", $"{hadoopURL}webhdfs/v1/{filePath}?op=LISTSTATUS", null, response => {
            responseHttpStatusCode = response.StatusCode;
            if (responseHttpStatusCode == HttpStatusCode.OK)
            {
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                HDFSResponse.FileStatuses fileStatuses = JsonConvert.DeserializeObject<HDFSResponse.FileStatuses>(jsonResponse);
                //Console.WriteLine(jsonResponse);
                files = fileStatuses.fileStatuses.fileStatus;
                // foreach(var i in fileStatuses.fileStatuses.fileStatus)
                // {
                //     Console.WriteLine(">" + i.pathSuffix);
                // }
            }
        });

        return files;
    }

    //Checksum
    public static async Task<HDFSResponse.FileChecksum> FileChecksum(string hadoopURL, string filePath)
    {
        HDFSResponse.FileChecksum fileChecksum = null;

        HDFSResponse.Redirection createFile = await HadoopAPI.CallHadoop<HDFSResponse.Redirection>("GET", $"{hadoopURL}webhdfs/v1/{filePath}?op=GETFILECHECKSUM&noredirect=true", null, HttpStatusCode.OK);
        if (createFile != null)
        {
            await CallHadoop("GET", createFile.Location, null, response => {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().Result;
                    fileChecksum = JsonConvert.DeserializeObject<HDFSResponse.FileChecksum>(jsonResponse);
                }
            });
        }

        return fileChecksum;
    }

    //FileStatus
    public static async Task<HDFSResponse.FileGetStatus> FileStatus(string hadoopURL, string filePath)
    {
        HDFSResponse.FileGetStatus fileStatus = null;
        HttpStatusCode responseHttpStatusCode = HttpStatusCode.NotFound;
        await CallHadoop("GET", $"{hadoopURL}webhdfs/v1/{filePath}?op=GETFILESTATUS", null, response => {
            responseHttpStatusCode = response.StatusCode;
            if (responseHttpStatusCode == HttpStatusCode.OK)
            {
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                //Console.WriteLine(jsonResponse);
                fileStatus = JsonConvert.DeserializeObject<HDFSResponse.FileGetStatus>(jsonResponse);
            }
        });

        return fileStatus;
    }
}
