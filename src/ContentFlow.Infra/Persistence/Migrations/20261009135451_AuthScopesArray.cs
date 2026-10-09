using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentFlow.Infra.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthScopesArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No implicit cast exists from varchar to text[], so an explicit
            // USING conversion is required (verified: 42804 without it).
            migrationBuilder.Sql(
                "ALTER TABLE api_keys ALTER COLUMN scopes TYPE text[] USING string_to_array(scopes, ' ');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE api_keys ALTER COLUMN scopes TYPE character varying(2000) USING array_to_string(scopes, ' ');");
        }
    }
}
