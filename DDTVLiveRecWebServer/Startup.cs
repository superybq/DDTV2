using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DDTVLiveRecWebServer
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/log", async context =>
                {
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    if (File.Exists("./LOG/DDTVLiveRecLog.out"))
                    {
                        Auxiliary.MMPU.�ļ�ɾ��ί��("./LOG/DDTVLiveRecLog.out.bak", "�����µ�log�ļ�1��ɾ���Ͼ�log�ļ�");
                        File.Copy("./LOG/DDTVLiveRecLog.out", "./LOG/DDTVLiveRecLog.out.bak");
                        await context.Response.WriteAsync(File.ReadAllText("./LOG/DDTVLiveRecLog.out.bak", System.Text.Encoding.UTF8));
                        Auxiliary.MMPU.�ļ�ɾ��ί��("./LOG/DDTVLiveRecLog.out.bak", "�����µ�log�ļ�2��ɾ���Ͼ�log�ļ�");
                        return;
                    }
                    else
                    {
                        await context.Response.WriteAsync("û�л�ȡ����־�ļ�����ȷ��DDTVLive��������");
                    }
                });
                endpoints.MapGet("/file", async context =>
                {
                    if(!Directory.Exists(Auxiliary.MMPU.����·��))
                    {
                        Directory.CreateDirectory(Auxiliary.MMPU.����·��);
                    }
                    string A = "��ǰ¼���ļ����ļ��б�:\r\n";
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    foreach (DirectoryInfo NextFolder1 in new DirectoryInfo(Auxiliary.MMPU.����·��).GetDirectories())
                    {
                        A = A + "\r\n" + NextFolder1.Name;
                        foreach (FileInfo NextFolder2 in new DirectoryInfo(Auxiliary.MMPU.����·�� + NextFolder1.Name).GetFiles())
                        {
                            A = A + "\r\n����" + Math.Ceiling(NextFolder2.Length / 1024.0 / 1024.0) + " MB |" + NextFolder2.Name;
                        }
                        A = A + "\r\n";
                    }
                    await context.Response.WriteAsync(A, System.Text.Encoding.UTF8);
                });
                endpoints.MapGet("/list", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(Auxiliary.InfoLog.DownloaderInfoPrintf(0), System.Text.Encoding.UTF8);
                });
                endpoints.MapGet("/wssinfo", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(Auxiliary.InfoLog.����WSS����״̬�б�(), System.Text.Encoding.UTF8);
                });
                //endpoints.MapGet("/login", async context =>
                //{
                //    context.Response.ContentType = "image/png";
                //    if(File.Exists("./BiliQR.png"))
                //    {
                //        await context.Response.SendFileAsync("./BiliQR.png"); 
                //    }
                //   else
                //    {
                //        await context.Response.WriteAsync("<a>��ά�����ʧ�ܣ����Ե�3���ˢ����ҳ,����ʧ�ܣ���鿴����̨�Ƿ����������Ϣ<a/>", System.Text.Encoding.UTF8);
                //    }
                //});
                endpoints.MapGet("/systeminfo", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(Auxiliary.InfoLog.GetSystemInfo());
                });
                endpoints.MapGet("/config", async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("���������������޸�����:<br/>(�޸ĺ�������DDTVLiveRec��Ч)<br/>" +
                        "<br/>�򿪵�Ļ/����/����¼�ƴ���IP:11419/config-DanmuRecOn" +
                        "<br/>�رյ�Ļ/����/����¼�ƴ���IP:11419/config-DanmuRecOff" +
                        "<br/>��DEBUGģʽ IP:11419/config-DebugOn" +
                        "<br/>�ر�DEBUGģʽ IP:11419/config-DebugOff");
                });
                endpoints.MapGet("/config-DanmuRecOn", async context =>
                {
                    Auxiliary.MMPU.¼�Ƶ�Ļ = true;
                    Auxiliary.MMPU.setFiles("RecordDanmu", "1");
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("�򿪵�Ļ/����/����¼�ƴ���ɹ�");
                });
                endpoints.MapGet("/config-DanmuRecOff", async context =>
                {
                    Auxiliary.MMPU.¼�Ƶ�Ļ = false;
                    Auxiliary.MMPU.setFiles("RecordDanmu", "0");
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("�رյ�Ļ/����/����¼�ƴ���ɹ�");
                });
                endpoints.MapGet("/config-DebugOn", async context =>
                {
                    Auxiliary.InfoLog.ClasslBool.Debug = true;
                    Auxiliary.InfoLog.ClasslBool.������ļ� = true;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("Debugģʽ��������ģʽ�»���log�ļ����ն��������log��Ϣ����ע���ļ����������Ĭ�Ϲر�debugģʽ");
                });
                endpoints.MapGet("/config-DebugOff", async context =>
                {
                    Auxiliary.InfoLog.ClasslBool.Debug = false;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("Debugģʽ�ѹر�");
                });
            });
        }
    }
}
