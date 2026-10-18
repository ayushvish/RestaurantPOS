using System;

namespace RestaurantPOS.API.Tests.Helpers;

public static class TestDataConstants
{
    public static readonly Guid RestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string RestaurantName = "Gourmet Palace";

    public static readonly Guid OutletId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public const string OutletName = "Main Dining Room";

    public static readonly Guid FloorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public const string FloorName = "Ground Floor";

    public static readonly Guid Table1Id = Guid.Parse("44444444-4444-4444-4444-444444444441");
    public const string Table1Number = "T-01";

    public static readonly Guid Table2Id = Guid.Parse("44444444-4444-4444-4444-444444444442");
    public const string Table2Number = "T-02";

    public static readonly Guid Table3Id = Guid.Parse("44444444-4444-4444-4444-444444444443");
    public const string Table3Number = "T-03";

    public static readonly Guid CategoryStartersId = Guid.Parse("55555555-5555-5555-5555-555555555551");
    public static readonly Guid CategoryMainsId = Guid.Parse("55555555-5555-5555-5555-555555555552");
    public static readonly Guid CategoryDessertsId = Guid.Parse("55555555-5555-5555-5555-555555555553");

    public static readonly Guid PaneerTikkaId = Guid.Parse("66666666-6666-6666-6666-666666666661");
    public static readonly Guid ButterChickenId = Guid.Parse("66666666-6666-6666-6666-666666666662");
    public static readonly Guid GulabJamunId = Guid.Parse("66666666-6666-6666-6666-666666666663");

    public static readonly Guid RawMaterialPaneerId = Guid.Parse("77777777-7777-7777-7777-777777777771");
    public static readonly Guid RawMaterialChickenId = Guid.Parse("77777777-7777-7777-7777-777777777772");
    public static readonly Guid RawMaterialMawaId = Guid.Parse("77777777-7777-7777-7777-777777777773");

    public static readonly Guid SuperAdminUserId = Guid.Parse("88888888-8888-8888-8888-888888888881");
    public static readonly Guid AdminUserId = Guid.Parse("88888888-8888-8888-8888-888888888882");
    public static readonly Guid CashierUserId = Guid.Parse("88888888-8888-8888-8888-888888888883");
    public static readonly Guid WaiterUserId = Guid.Parse("88888888-8888-8888-8888-888888888884");

    public const string DefaultPassword = "Password123!";
    public const string CashierPin = "1234";
    public const string WaiterPin = "5678";
}
