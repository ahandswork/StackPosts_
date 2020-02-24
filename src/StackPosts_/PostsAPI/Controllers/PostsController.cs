using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PostsAPI.Data;
using PostsAPI.Data.Entities;
using PostsAPI.Hubs;

namespace PostsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase 
    {
        private readonly IHubContext<PostHub, IPostHub> _hubContext;

        public PostsController(IHubContext<PostHub, IPostHub> postHub)
        {
            _hubContext = postHub;
        }

        public static ConcurrentBag<Post> posts = new ConcurrentBag<Post>
        {
            new Post
            {
                Id = Guid.Parse("b00c58c0-df00-49ac-ae85-0a135f75e01b"),
                Title = "Welcome to the example Post",
                Body = "Welcome to this demonstration of making a Stack Overflow clone using ASP.Net Core 2.2 and Vue.js 2.6",
                Score = 4,
                Deleted = false,
                DatePosted = DateTime.Now.AddDays(2),
                Replies = new List<Reply>{new Reply { Body =  "Super exciting reply example here!", Score = 1 }}
            }
        };

        [HttpGet]
        public IEnumerable GetPosts()
        {
            return posts.Where(t => !t.Deleted).Select(p => new { 
                Id = p.Id,
                    Title = p.Title,
                    Body = p.Body,
                    Score = p.Score,
                    DatePosted = p.DatePosted,
                    ReplyCount = p.Replies.Count
            });
        }

        [HttpGet("{id}")]
        public ActionResult GetPost(Guid id)
        {
            var post = posts.SingleOrDefault(p => p.Id == id);
            if (post == null) return NotFound();

            return new JsonResult(post);
        }

        [HttpPost]
        public Post AddPost([FromBody]Post post)
        {
            post.Id = Guid.NewGuid();
            post.Deleted = false;
            post.Replies = new List<Reply>();
            posts.Add(post);

            return post;
        }

        [HttpPost("{id}/reply")]
        public async Task<ActionResult> AddReplyAsync(Guid id, [FromBody]Reply reply)
        {
            var post = posts.SingleOrDefault(t => t.Id == id && !t.Deleted);
            if (post == null) return NotFound();

            reply.Id = Guid.NewGuid();
            reply.PostId = id;
            reply.Deleted = false;
            post.Replies.Add(reply);

            await _hubContext.Clients.Group(id.ToString()).ReplyAdded(reply);
            await _hubContext.Clients.All.ReplyCountChange(post.Id, post.Replies.Count);

            return new JsonResult(reply);
        }

        [HttpPatch("{id}/upvote")]
        public async Task<ActionResult> UpvotePostAsync(Guid id)
        {
            var post = posts.SingleOrDefault(t => t.Id == id);
            if (post == null) return NotFound();

            // Warning, this is not thread-safe. Use interlocked methods.
            post.Score++;

            await _hubContext.Clients.All.PostScoreChange(post.Id, post.Score);

            return new JsonResult(post);
        }

        [HttpPatch("{id}/downvote")]
        public async Task<ActionResult> DownvotePostAsync(Guid id)
        {
            var post = posts.SingleOrDefault(t => t.Id == id);
            if (post == null) return NotFound();

            post.Score--;

            await _hubContext.Clients.All.PostScoreChange(post.Id, post.Score);

            return new JsonResult(post);
        }
    }
}