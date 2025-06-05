using Quartz;
using Quartz.Impl;
using System;

namespace MBS.Web.Portal.Services
{
    public class JobScheduler
    {
        public static void Start(string triggerExpression1, string triggerExpression2)
        {
            //IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
            //scheduler.Start();

            //IJobDetail job1 = JobBuilder.Create<ReturnRetrivalJob>().Build();

            ////ITrigger trigger1 = TriggerBuilder.Create().StartNow().Build();

            //ITrigger trigger1 = TriggerBuilder.Create()
            //    .WithCronSchedule(triggerExpression1)
            //    .Build();

            //scheduler.ScheduleJob(job1, trigger1);

            //IJobDetail job2 = JobBuilder.Create<SubmitPendingClaims>().Build();

            ////ITrigger trigger2 = TriggerBuilder.Create().StartNow().Build();

            //ITrigger trigger2 = TriggerBuilder.Create()
            //    .WithCronSchedule(triggerExpression2)
            //    .Build();

            //scheduler.ScheduleJob(job2, trigger2);
        }
    }
}