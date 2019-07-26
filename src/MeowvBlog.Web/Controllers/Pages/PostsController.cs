﻿using Microsoft.AspNetCore.Mvc;

namespace MeowvBlog.Web.Controllers.Pages
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PostsController : Controller
    {
        [Route("/posts")]
        [Route("/posts/page/{page:int}")]
        public IActionResult Index() => View();

        [Route("/post/{year:int:min(2014):max(2024):length(4)}/{month:int:range(1,12)}/{day:int:range(1,31)}/{url}")]
        public IActionResult Detail() => View();
    }
}