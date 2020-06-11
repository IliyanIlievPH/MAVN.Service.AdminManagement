﻿using Autofac;
using JetBrains.Annotations;
using Lykke.Sdk;
using MAVN.Service.Credentials.Client;
using MAVN.Service.AdminManagement.Domain.Services;
using MAVN.Service.AdminManagement.DomainServices;
using MAVN.Service.AdminManagement.Managers;
using MAVN.Service.AdminManagement.Settings;
using MAVN.Service.CustomerProfile.Client;
using MAVN.Service.Sessions.Client;
using Lykke.SettingsReader;
using StackExchange.Redis;
using MAVN.Service.AdminManagement.Domain.Models;

namespace MAVN.Service.AdminManagement.Modules
{
    [UsedImplicitly]
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterSessionsServiceClient(_appSettings.CurrentValue.SessionsService);
            builder.RegisterCredentialsClient(_appSettings.CurrentValue.CredentialsService);
            builder.RegisterCustomerProfileClient(_appSettings.CurrentValue.CustomerProfileServiceClient);

            builder.RegisterType<AuthService>()
                .As<IAuthService>()
                .SingleInstance();

            builder.RegisterType<AdminUserService>()
                .As<IAdminUserService>()
                .SingleInstance();

            builder.RegisterType<AutofillValuesService>()
                .As<IAutofillValuesService>()
                .SingleInstance();

            builder.RegisterType<PermissionsService>()
                .As<IPermissionsService>()
                .SingleInstance();

            // DO NOT UPDATE REDIS TO 2.* Right now it works worse as 1.2
            builder.Register(context =>
            {
                var connectionMultiplexer = ConnectionMultiplexer.Connect(_appSettings.CurrentValue.AdminManagementService.Redis.ConnectionString);
                connectionMultiplexer.IncludeDetailInExceptions = false;
                return connectionMultiplexer;
            }).As<IConnectionMultiplexer>().SingleInstance();
            
            builder.RegisterType<PermissionsCache>()
                .As<IPermissionsCache>()
                .WithParameter("redisInstanceName", _appSettings.CurrentValue.AdminManagementService.Redis.InstanceName)
                .WithParameter("ttl", _appSettings.CurrentValue.AdminManagementService.Redis.Ttl)
                .SingleInstance();

            #region Verification

            builder.RegisterType<EmailVerificationService>()
                .As<IEmailVerificationService>()
                .SingleInstance();

            var callRateLimitSettingsDto = new CallRateLimitSettingsDto
            {
                EmailVerificationCallsMonitoredPeriod = _appSettings.CurrentValue.AdminManagementService.LimitationSettings.EmailVerificationCallsMonitoredPeriod,
                EmailVerificationMaxAllowedRequestsNumber = _appSettings.CurrentValue.AdminManagementService.LimitationSettings.EmailVerificationMaxAllowedRequestsNumber,
            };

            builder.RegisterType<CallRateLimiterService>()
                .As<ICallRateLimiterService>()
                .WithParameter(TypedParameter.From(callRateLimitSettingsDto))
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.AdminManagementService.Redis.InstanceName));

            #endregion

            builder.RegisterType<NotificationsService>()
                .As<INotificationsService>()
                .WithParameter("backOfficeUrl", _appSettings.CurrentValue.AdminManagementService.BackOfficeLink)
                .WithParameter("adminCreatedEmailTemplateId", _appSettings.CurrentValue.AdminManagementService.AdminCreatedEmail.EmailTemplateId)
                .WithParameter("adminCreatedEmailSubjectTemplateId", _appSettings.CurrentValue.AdminManagementService.AdminCreatedEmail.SubjectTemplateId)
                .WithParameter("adminCreatedVerificationLinkPath", _appSettings.CurrentValue.AdminManagementService.AdminCreatedEmail.VerificationLinkPath)
                .WithParameter("adminPasswordResetEmailTemplateId", _appSettings.CurrentValue.AdminManagementService.PasswordResetEmail.EmailTemplateId)
                .WithParameter("adminPasswordResetEmailSubjectTemplateId", _appSettings.CurrentValue.AdminManagementService.PasswordResetEmail.SubjectTemplateId)
                .WithParameter("partnerAdminWelcomeEmailTemplateId", _appSettings.CurrentValue.AdminManagementService.PartnerAdminWelcomeEmail.EmailTemplateId)
                .WithParameter("partnerAdminWelcomeEmailSubjectTemplateId", _appSettings.CurrentValue.AdminManagementService.PartnerAdminWelcomeEmail.SubjectTemplateId)
                .WithParameter("partnerAdminWelcomeLinkPath", _appSettings.CurrentValue.AdminManagementService.PartnerAdminWelcomeEmail.PartnersLinkPath)
                .SingleInstance();
            
            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();
            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();
        }
    }
}
