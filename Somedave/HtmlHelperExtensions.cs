﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FluentBootstrap;
using FluentBootstrap.Buttons;
using Somedave.Controllers;

namespace Somedave
{
    public static class HtmlHelperExtensions
    {
        public static LinkButton<TModel> TagButton<TModel>(this HtmlHelper<TModel> helper, string tag, int? count = null, ButtonStyle buttonStyle = ButtonStyle.Default)
        {
            UrlHelper url = new UrlHelper(helper.ViewContext.RequestContext);
            return helper.Bootstrap().LinkButton(
                string.Format(" {0}{1}", tag, count == null ? string.Empty : string.Format(" <span class='badge'>{0}</span>", count)), 
                url.Action(MVC.Blog.Tags(tag.ToLowerInvariant().Replace(' ', '-'))),
                buttonStyle)
                .BtnSm()
                .AddCss("tag-button", "icon-tag-2");
        }

        public static MvcHtmlString Post<TModel>(this HtmlHelper<TModel> helper, string linkText, Func<BlogController.ViewsClass._PostsClass, string> view)
        {
            return helper.ActionLink(linkText, MVC.Blog.Posts(System.IO.Path.GetFileNameWithoutExtension(view(MVC.Blog.Views.Posts))));
        }
    }
}