﻿using Meowv.Blog.Authorize.OAuth;
using Meowv.Blog.Authorize.OAuth.Impl;
using Meowv.Blog.Domain.Users;
using Meowv.Blog.Dto.Authorize.Params;
using Meowv.Blog.Options;
using Meowv.Blog.Response;
using Meowv.Blog.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Meowv.Blog.Authorize.Impl
{
    public class AuthorizeService : ServiceBase, IAuthorizeService
    {
        private readonly JwtOptions _jwtOption;
        private readonly OAuthGithubService _githubService;
        private readonly OAuthGiteeService _giteeService;
        private readonly OAuthAlipayService _alipayService;
        private readonly OAuthDingtalkService _dingtalkService;

        public AuthorizeService(IOptions<JwtOptions> jwtOption,
                                OAuthGithubService githubService,
                                OAuthGiteeService giteeService,
                                OAuthAlipayService alipayService,
                                OAuthDingtalkService dingtalkService)
        {
            _jwtOption = jwtOption.Value;
            _githubService = githubService;
            _giteeService = giteeService;
            _alipayService = alipayService;
            _dingtalkService = dingtalkService;
        }

        /// <summary>
        /// Get authorize url.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [Route("api/meowv/oauth/{type}")]
        public async Task<BlogResponse<string>> GetAuthorizeUrlAsync(string type)
        {
            var state = StateManager.Instance.Get();

            var response = new BlogResponse<string>
            {
                Result = type switch
                {
                    "github" => await _githubService.GetAuthorizeUrl(state),
                    "gitee" => await _giteeService.GetAuthorizeUrl(state),
                    "alipay" => await _alipayService.GetAuthorizeUrl(state),
                    "dingtalk" => await _dingtalkService.GetAuthorizeUrl(state),
                    _ => throw new NotImplementedException($"Not implemented:{type}")
                }
            };

            return response;
        }

        /// <summary>
        /// Generate token by <paramref name="type"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/meowv/oauth/{type}/token")]
        public async Task<BlogResponse<string>> GenerateTokenAsync(string type, string code, string state)
        {
            var response = new BlogResponse<string>();

            if (!StateManager.IsExist(state))
            {
                response.IsFailed("Request failed.");
                return response;
            }

            StateManager.Remove(state);

            var token = type switch
            {
                "github" => GenerateToken(await _githubService.GetUserByOAuthAsync(type, code, state)),
                "gitee" => GenerateToken(await _giteeService.GetUserByOAuthAsync(type, code, state)),
                "alipay" => GenerateToken(await _alipayService.GetUserByOAuthAsync(type, code, state)),
                "dingtalk" => GenerateToken(await _dingtalkService.GetUserByOAuthAsync(type, code, state)),
                _ => throw new NotImplementedException($"Not implemented:{type}")
            };

            response.IsSuccess(token);
            return response;
        }

        /// <summary>
        /// Generate token by account.
        /// </summary>
        /// <param name="userService"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        [Route("api/meowv/oauth/account/token")]
        public async Task<BlogResponse<string>> GenerateTokenAsync([FromServices] IUserService userService, AccountInput input)
        {
            var response = new BlogResponse<string>();

            var user = await userService.VerifyByAccountAsync(input.Username, input.Password);
            var token = GenerateToken(user);

            response.IsSuccess(token);
            return await Task.FromResult(response);
        }

        private string GenerateToken(User user)
        {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("avatar", user.Avatar),
                new Claim(JwtRegisteredClaimNames.Exp, $"{new DateTimeOffset(DateTime.Now.AddMinutes(_jwtOption.Expires)).ToUnixTimeSeconds()}"),
                new Claim(JwtRegisteredClaimNames.Nbf, $"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}")
            };

            var key = new SymmetricSecurityKey(_jwtOption.SigningKey.GetBytes());
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var securityToken = new JwtSecurityToken(
                issuer: _jwtOption.Issuer,
                audience: _jwtOption.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtOption.Expires),
                signingCredentials: creds);

            var token = new JwtSecurityTokenHandler().WriteToken(securityToken);
            return token;
        }
    }
}