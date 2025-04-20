namespace SeoManagement.API.Middleware
{
	public class ExceptionMiddleware
	{

		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			try
			{
				await next(context);
			}
			catch (KeyNotFoundException ex)
			{
				context.Response.StatusCode = 404;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch (ArgumentException ex)
			{
				context.Response.StatusCode = 400;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch (Exception ex)
			{
				context.Response.StatusCode = 500;
				await context.Response.WriteAsJsonAsync(new
				{
					error = "Internal server error",
					detail = ex.Message
				});
			}
		}
	}
}
