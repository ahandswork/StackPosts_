using System;
using System.Collections.Generic;

namespace PostsAPI.Models
{
    public class Post
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int Score { get; set; } // upvote & downvote tracking 
        public bool Deleted { get; set; }
        public DateTime DatePosted { get; set; }
        public List<Reply> Replies;
    }
}