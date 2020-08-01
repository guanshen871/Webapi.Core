﻿using Autofac;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using Webapi.Core.Common.Helper;
using Webapi.Core.Common.Redis;
using Webapi.Core.Filter;
using Webapi.Core.JsonConv;
using Webapi.Core.Middleware;
using Webapi.Core.Repository.Sugar;
using Webapi.Core.SetUp;

namespace Webapi.Core
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //注册appsettings读取类
            services.AddSingleton(new Appsettings(Configuration));

            //注册Redis
            services.AddSingleton<IRedisCacheManager, RedisCacheManager>();

            //数据库配置
            BaseDBConfig.ConnectionString = Configuration.GetSection("AppSettings:ConnectionString").Value;

            //var text = Appsettings.app(new string[] { "AppSettings", "ConnectionString" });
            //Console.WriteLine($"ConnectionString:{text}");
            //Console.ReadLine();

            //注册Swagger
            services.AddSwaggerSetup();

            //jwt授权验证
            services.AddAuthorizationSetup();

            //注册automapper
            services.AddAutoMapper(typeof(Startup));

            services.AddControllers(option =>
            {
                option.Filters.Add(typeof(GlobalExceptionsFilter));
            }).AddJsonOptions(option =>
            {
                //空的字段不返回
                option.JsonSerializerOptions.IgnoreNullValues = true;
                //返回json小写
                option.JsonSerializerOptions.PropertyNamingPolicy = new LowercasePolicy();


            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new AutofacModuleRegister());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/V1/swagger.json", "WebApi.Core V1");

                //路径配置，设置为空，表示直接在根域名（localhost:8001）访问该文件,注意localhost:8001/swagger是访问不到的，去launchSettings.json把launchUrl去掉，如果你想换一个路径，直接写名字即可，比如直接写c.RoutePrefix = "doc";
                c.RoutePrefix = "";
            });
            app.UseCustomExceptionMiddleware();
            //注意中间件的顺序，UseRouting放在最前边，UseAuthentication在UseAuthorization前边
            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
