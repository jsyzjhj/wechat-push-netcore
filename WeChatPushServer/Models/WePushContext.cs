using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WeChatPushServer.Models
{
    public class WePushContext : DbContext
    {
        public DbSet<UserApiKey> UserApiKeys { get; set; }
        public DbSet<Message>  Messages { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=wupush.db");
        }
    }

    public class UserApiKey
    {
        [Key]
        public string OpenId { get; set; }
        public string ApiKey { get; set; }
    }
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime SendTime { get; set; }
        public string ApiKey { get; set; }
    }
}
