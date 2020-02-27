﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SendMailService;
using Serilog;

namespace AspNetCoreLocalLog.EmailExceptions
{
    public static class ConfigureExceptionMiddleware
    {
        public static void AddExceptionLogger(this IServiceCollection collection)
        {
            collection.AddSingleton<ExceptionHandlerMiddleware, ExceptionHandlerMiddleware>();
        }

        public static IConfigureExceptionMiddleware UseExceptionLogger(this IApplicationBuilder builder)
        {
            var exceptionLogger = builder.ApplicationServices.GetService<ExceptionHandlerMiddleware>();
            builder.Use(exceptionLogger.Process);
            return exceptionLogger;
        }
    }
    public interface IConfigureExceptionMiddleware
    {
        IConfigureExceptionMiddleware WithEmailTarget(string email);
    }
    public class ExceptionHandlerMiddleware: IConfigureExceptionMiddleware
    {
        private readonly ISendEmailService email;
        private readonly ILogger logger;
        private readonly List<string> targetEmails = new List<string>();

        public ExceptionHandlerMiddleware(ISendEmailService email, ILogger logger)
        {
            this.email = email;
            this.logger = logger;
        }

        public async Task Process(HttpContext context, Func<Task> next)
        {
            try
            {
                await next();
            }
            catch (Exception e)
            {
                await HandleException(ExceptionPrinter.ExceptionToText(e, context));
                throw;
            }
        }


        private Task HandleException(string message)
        {
            logger.Error(message);
            return SendExceptionEmails(message);
        }

        private async Task SendExceptionEmails(string message)
        {
            foreach (var addr in targetEmails)
            {
                await email.SendEmail(addr, "Exception Thrown",
                    $"<pre>{message}</pre>");
            }
        }

        public IConfigureExceptionMiddleware WithEmailTarget(string email)
        {
            targetEmails.Add(email);
            return this;
        }
    }
}