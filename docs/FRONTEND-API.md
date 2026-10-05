# NileTechno API — Frontend Reference

Base URL (production): `https://my-back-end-jayl.onrender.com`
Base URL (local dev): `http://localhost:5080`

## القواعد العامة
- كل الـ JSON **camelCase**.
- المصادقة: هيدر `Authorization: Bearer <accessToken>` (صلاحية 60 دقيقة). الحسابات العادية role بتاعها `User`؛ حسابات الأدمن (محددَة بإيميلها في إعدادات السيرفر) تاخد `Admin`/`MainAdmin` وقت اللوجين.
- **هوية العميل بتُقرأ من التوكن:** كل نقاط `/api/cart` و`/api/wishlist` و`/api/addresses` لازم معها Bearer (بدونه 401)، وقيمة `userId` في الـ body/query **لا تُستخدم** لحساب عادي — السيرفر بيشتغل على حساب التوكن نفسه. `Admin`/`MainAdmin` بس يقدر يحدد `userId` حساب تاني. الاستثناءات المتعمّدة: التقاط السلة المتروكة `/api/abandoned-carts` (POST) والشراء `/api/orders` (POST) يفضلوا متاحة للزوار بدون توكن.
- الأخطاء: إما `{ title, status, errors? }` أو `{ errors: ["..."] }` برسائل عربية.
- أي حاجة اسمها `productId` في الـ API ده هي **كود الصنف** `itemCode` (زي `"12668"`)، مش UUID.
- المنتجات والتصنيفات والاستوك بتُقرأ من `EC_Products`/`EC_Groups` اللي بتتحدث من الـ ERP كل دقيقتين — أي صنف `Status=0` أو مجموعته `Status=0` لا يظهر أبدًا للعميل: لا في القوائم، ولا في الكارت، ولا في المفضلة، وإضافته للسلة أو طلبه في أوردر يرجّع 400.
- الجداول المستخدمة: EC_Products, EC_Groups, Ec_Orders, Ec_OrderItems, Ec_OrderHistoryEntries, Ec_Reviews, Ec_Cart, Ec_WishlistItems, Ec_UserAddresses, Ec_Coupons, Ec_ShippingZones, Ec_StoreSettingsList, Ec_ActivityLogs, Ec_AbandonedCarts/Items, Ec_AnalyticsSearches/Views, Ec_StockOverrides, Ec_ErpPostings, Ec_LoginAccounts. (تم حذف Ec_CartItems وEc_Categories وEC_Rating.)

---

## 1) Health
| Method | Path | Auth | الوصف |
|---|---|---|---|
| GET | `/api/health` | — | `{status:"ok", database:"connected", timeUtc}` |

## 2) Auth (Ec_LoginAccounts)
| Method | Path | Auth | Body / Params | الرد |
|---|---|---|---|---|
| POST | `/api/auth/register` | — | `{email, password, fullName}` | **`AuthResponse` (تحت — تسجيل دخول تلقائي)** أو fallback `{message,userId,emailConfirmed:true}`؛ و400 `{errors:[...]}` لو موجود |
| POST | `/api/auth/login` | — | `{email, password}` | AuthResponse (تحت) |
| POST | `/api/auth/google` | — | `{accessToken \| idToken}` (Google GSI) | AuthResponse أو 401 |
| POST | `/api/auth/refresh-token` | — | `{refreshToken}` | AuthResponse كامل جديد (الـ refresh صلاحيته 30 يوم وبيتدوّر كل مرة — القديم بيبطل فورًا) |
| POST | `/api/auth/logout` | Bearer | — | `{message:"تم تسجيل الخروج بنجاح."}` ويمسح الـ refresh token |
| GET | `/api/auth/check-email?email=` | — | — | `{exists: bool}` |
| POST | `/api/auth/forgot-password` | — | `{email}` | 200 دايمًا برسالة محايدة + سيرسل إيميل إعادة الضبط |
| POST | `/api/auth/reset-password` | — | `{email, token, newPassword}` | `{message}` |
| GET | `/api/auth/confirm-email?email=&token=` | — | — | رسالة تأكيد |
| POST | `/api/auth/resend-verification` | — | `{email}` | 200 محايد |
| GET | `/api/auth/profile` | Bearer | — | `{userId,email,fullName,phone,loyaltyPoints,authProvider,emailConfirmed,createdAt,lastLoginAt}` |
| PUT | `/api/auth/profile` | Bearer | `{fullName?, phone?}` | `{success:true}` |
| POST | `/api/auth/change-password` | Bearer | `{currentPassword, newPassword}` (≥6) | `{success:true}` أو 400 `{errors}` |

**AuthResponse (flat — نفس الرد في login/register/google/refresh):**
```json
{ "userId": 1, "email": "...", "fullName": "...", "roles": ["User"],
  "accessToken": "jwt", "accessTokenExpiresAtUtc": "...", "refreshToken": "...",
  "loyaltyPoints": 100, "phone": null, "createdAt": "...", "authProvider": "Password|Google" }
```
> الريجستر بيرد نفس الرد ده كاملًا فور الإنشاء (تسجيل دخول تلقائي من غير خطوة login): `setTokens(accessToken, refreshToken)` ثم خزّن باقي الحقول.
> حسابات Google لا تسجّل بكلمة مرور؛ كلمة مرورها تتغيّر من `/api/auth/change-password` فقط لحسابات Password.

## 3) المنتجات والتصنيفات (المصدر: EC_Products، filter Status=1)
| Method | Path | Auth | Params | الرد |
|---|---|---|---|---|
| GET | `/api/products` | — | `groupId, search, page(=1), pageSize(=0), deviceType` أو هيدر `X-Device-Type` | PaginatedList (تحت). `pageSize=0` ⇒ حسب الجهاز: mobile 20 / tablet 30 / desktop 50، وmax 100 |
| GET | `/api/products/{itemCode}` | — | — | كارت منتج واحد أو 404 `{title:"الصنف غير موجود"}` |
| GET | `/api/categories` | — | — | `[{groupId, groupName, itemCount}]` (غير متضمنة جروب Status=0) |

**كارت المنتج:**
```json
{ "itemCode":"12668", "itemId":"66570", "itemName":"اسم الصنف",
  "groupId":"107", "groupName":"جاف", "stock":1061.00, "price":15.00,
  "image":"", "description":"" }
```
> `image`/`description` من الأعمدة اليدوية في EC_Products (الـ ERP sync ما يلمسهمش) — املأهم بالجدول يظهروا فورًا.

**PaginatedList:**
```json
{ "items":[...], "pageNumber":1, "pageSize":20, "totalPages":572,
  "totalCount":11431, "hasPreviousPage":false, "hasNextPage":true }
```

## 4) الاستوك
| Method | Path | Auth | ملاحظات |
|---|---|---|---|
| GET | `/api/product-stock` | — | قاموس كامل `{"12668":1061.00, ...}` (من EC_Products + overrides) |
| GET | `/api/product-stock/{itemCode}` | — | `{productId, stock}` (غير موجود ⇒ 0) |
| POST | `/api/product-stock/{itemCode}` | **Admin** | `{stock:number}` write override — **الـ sync كل دقيقتين بيرجّع القيمة من ERP**، فاستخدم `/api/orders` لتقليل الاستوك عمليًا |
| POST | `/api/product-stock/decrement` | **Admin** | `{items:[{productId,quantity}]}` → `{success, stocks:{map}}` (مش بيقلّل تحت الصفر) |
| GET | `/api/stock?pageNumber=1&pageSize=50&groupId=&storeCode=&search=` | **Admin** | كشف استوك تفصيلي لكل مخزن من ERP الحي: PaginatedList<{itemCode,itemName,transPkgQty1,reorderQty,storeCode,storeName,groupId,groupName}> (pageSize 1–200) |

## 5) الأوردرات (Ec_Orders — int ids)
| Method | Path | Auth | Body | الرد |
|---|---|---|---|---|
| GET | `/api/orders` | — | — | مصفوفة أوردرات كاملة، الأحدث أولًا (لوحة الأدمن) |
| GET | `/api/orders/user/{userId}` | **Bearer** | — | أوردرات العميل ده بس — صاحبه أو Admin؛ غيرهما 403، ومن غير توكن 401 |
| GET | `/api/orders/{id}` | **Bearer** | `id` رقم أو `ORD-...` | أوردر واحد — صاحبه (بحسابه أو بإيميله) أو Admin؛ غيرهما 403، مفقود 404 |
| POST | `/api/orders` | — (زائر) / Bearer | payload الشراء (تحت — حقول إجبارية). مع توكن الأوردر بيتنسب لحساب التوكن و`userId` الـ body لا يُستخدم؛ زائر بدون توكن ياخد `userId` من الـ body | `{success:true, order}` أو 400 `{success:false, errors:[...]}` |
| POST | `/api/orders/update-status` | **Admin** | `{orderId, newStatus, cancelReason?}` — `orderId` يقبل الرقم أو رقم الأوردر "ORD-..." | `{success, order, emailSent}` |
| POST | `/api/orders/bulk-update-status` | **Admin** | `{orderIds:[...], newStatus, cancelReason?}` | `{success, updatedCount, orders[]}` |
| POST | `/api/orders/update-note` | **Admin** | `{orderId, note}` | `{success, order}` + سطر history |
| DELETE | `/api/orders/{orderId}` | **Admin** | — | `{success}` (يحذف الـ items والـ history بالـ cascade) |

> **ERP side-effect (من 2026-09-29، بدون تغيير في أي عقد):** الإنشاء يفتح فاتورة ERP تلقائيًا (TransType=3) فيُخصم المخزون ويظهر في المتجر خلال ~دقيقتين؛ CANCELED/REFUNDED (فردي أو جماعي) والحذف يعقّدون الفاتورة فيرجع المخزون؛ التنشيط من حالة ملغاة ينشرها مجددًا. فشل الفاتورة لا يُفشل الطلب أبدًا (outbox + retry).

**payload إنشاء أوردر — الإجباري من 2026-09-30: `customerName` + `customerPhone` + `governorate` + عنوان (أي من `address`/`addressDetails`/`customerAddress`) + `items` (منتج واحد على الأقل، `productId` و`quantity ≥ 1`)؛ والنقص بيرجع 400 `{success:false, errors:[...]}` برسائل عربية):**
```json
{ "userId":"5", "customerName":"", "customerPhone":"", "customerEmail":"",
  "customerAddress":"", "customerNotes":"", "governorate":"",
  "coordinates":{"lat":30.0,"lng":31.3},
  "paymentMethod":"الدفع عند الاستلام كاش",
  "couponCode":"E2E10",
  "items":[{"productId":"12668","name":"","price":15,"quantity":2,"image":"","selectedColor":null,"selectedSize":null}],
  "total":110, "subtotal":100, "shippingCost":10, "discountAmount":0 }
```
**كائن الأوردر في الرد:** `{id, orderNumber, userId, status, cancelReason, internalNote, customerName, customerEmail, customerPhone, customerNotes, governorate, customerAddress, addressDetails, latitude, longitude, paymentMethod, couponCode, discountAmount, subtotal, shippingCost, total, date, createdAt, updatedAt, items[], history[]}` وكل items ليها `{productId,name,price,quantity,image,selectedColor,selectedSize}` وhistory `{action,actor,type,newStatus,timestamp}`.

**حالات الأوردر (status في الرد UPPER_SNAKE):** `PENDING, PROCESSING, PACKED, SHIPPED, OUT_FOR_DELIVERY, DELIVERED, CANCELED, REFUNDED` — القارئ متسامح: `received/confirmed/in-transit/cancelled/completed...` تتفهم.

## 6) التقييمات (Ec_Reviews)
| Method | Path | Auth | Body/Params | الرد |
|---|---|---|---|---|
| GET | `/api/custom-reviews?productId=&all=true` | — | بدون `all` = المعتمد فقط | `[{id, externalId, productId, orderId, rating, comment, customerName, approved, date, createdAt}]` |
| POST | `/api/custom-reviews` | — | `{productId, orderId?, rating(1-5), comment, customerName}` | `{success, review}` — بيدخل `approved:false` (id هنا externalId نصي) |
| POST | `/api/custom-reviews/{id}/approve` | **Admin** | `id` = externalId أو Guid | `{success}` |
| DELETE | `/api/custom-reviews/{id}` | **Admin** | — | `{success}` |

## 7) السلة (Ec_Cart — userId = اي دي الحساب، productId = كود الصنف، id = int)
> كل النقاط دي **Bearer إجباري** (بدونه 401)، و`userId` بيتاخد من التوكن — قيمته في الـ body/query تُستخدم فقط لو المرسل `Admin`/`MainAdmin`.

| Method | Path | Auth | Body/Params | الرد |
|---|---|---|---|---|
| GET | `/api/cart?userId=` | **Bearer** | `userId` اختياري (أدمن فقط) | `[{id(int), productId, quantity, color, size, product:{id,itemCode,name,title,price,image,stock,groupId,category}}]` |
| POST | `/api/cart` | **Bearer** | `{userId?, productId, quantity, color?, size?}` | `{success}` upsert بنفس مفتاح (product+color+size) — `400 {error}` لو الكود مش في الكتالوج أو موقوف (Status=0) |
| POST | `/api/cart/sync` | **Bearer** | `{userId?, items:[{productId,quantity,color,size}]}` | `{success, replaced, removed}` — يستبدل السلة بالكامل (مضاف/محذوف/تحديث كمية) |
| DELETE | `/api/cart/{itemId}` (int) | **Bearer** | — | `{success}` أو `404 {error}` — أي صف مملوك لحساب تاني بيرجع `404` (شرط الملكية مفروض داخل استعلام SQL)؛ الأدمن بيشيل بأي id |
| DELETE | `/api/cart?userId=` | **Bearer** | `userId` اختياري (أدمن فقط) | تفريغ السلة `{success, removed}` |

> التخزين: `ProductID` = اي دي المنتج في EC_Products، و`ProductServerId` = ItemId بتاع ERP، واللوون/المقاس في عمود `Notes` كـ JSON — العقد الخارجي كما هو عدا الـ id صار number بدل GUID.
>
> **دورة حياة الصف (soft-delete):** حذف صنف من السلة (DELETE أو sync) بيحوّل صف `Ec_Cart` لـ `Status=0` **من غير ما يتمسح** — يفضل في الجدول و`OrderID` فاضي. لما العميل يكمل أوردر، كل صفوف سلة المرئية بتاخده `OrderID` الأوردر الجديد وبتختفي من `GET /api/cart`. يعني: `Status=0, OrderID NULL` = اتشال من السلة، `OrderID NOT NULL` = اتحول لأوردر. الإضافة لو صف مخفي بنفس المفتاح (منتج+لوون+مقاس) موجود — بيترجع نشيط بنفس الـ id بدل Duplicate.

## 8) المفضلة (Ec_WishlistItems)
> **Bearer إجباري** على كل النقاط (بدونه 401)، و`userId` يُقرأ من التوكن — قيمته في الطلب تُحترم فقط لو المرسل `Admin`/`MainAdmin`.

| Method | Path | Auth | Body/Params |
|---|---|---|---|
| GET | `/api/wishlist?userId=` | **Bearer** | `[{id, productId, product:{نفس شكل الكارت}}]` — الأصناف الموقوفة (Status=0) لا تظهر |
| POST | `/api/wishlist` | **Bearer** | `{userId?, productId}` (idempotent) — `productId` مطلوب وإلا 400 |
| POST | `/api/wishlist/sync` | **Bearer** | `{userId?, productIds:["993", ...]}` استبدال كامل |
| DELETE | `/api/wishlist?userId=&productId=` | **Bearer** | حذف صنف من مفضلة **المرسل نفسه** — `productId` مطلوب، وما بقيش ممكن تعدّي على قائمة حساب تاني |

## 9) العناوين (Ec_UserAddresses)
> **Bearer إجباري** على كل النقاط (بدونه 401). **مفيش `id` ولا `userId` في الـ body**: الـ id بيولّده SQL (`NEWSEQUENTIALID`)، وصاحب العنوان بيتقري من التوكن. `userId` في الـ query بيشتغل فقط لو المرسل `Admin`/`MainAdmin` وده للقراءة/الحذف.

| Method | Path | Auth | Body/Params |
|---|---|---|---|
| GET | `/api/addresses?userId=` | **Bearer** | `[{id(GUID), userId, label, governorate, details, latitude, longitude, isDefault, createdAt}]` |
| POST | `/api/addresses` | **Bearer** | `{label, governorate, details, latitude?, longitude?, isDefault}` → `{success, address}`. أول عنوان في الحساب بياخد `isDefault:true` لوحده حتى لو الطلب false |
| PUT | `/api/addresses/{id}` | **Bearer** | نفس body الإنشاء. عنوان حساب تاني = **404** |
| POST | `/api/addresses/sync` | **Bearer** | `{addresses:[{id?, label, governorate, details, latitude?, longitude?, isDefault}]}` يستبدل عناوين **حسابك** بالكامل (أي عنوان محفوظ ملهوش في القائمة **بيتحذف**؛ `id:null` = جديد؛ أول عنصر remaining default لو مفيش flag) |
| PUT | `/api/addresses/sync` | **Bearer** | نفس اللى فوق لكن الـ body مصفوفة مباشرة بدون غلاف `{addresses}` |
| DELETE | `/api/addresses/{id}?userId=` | **Bearer** | حذف — عنوان حساب تاني بيرجع `404` |

## 10) الكوبونات (Ec_Coupons)
| Method | Path | Auth | Body/Params |
|---|---|---|---|
| GET | `/api/coupons` | — | — | قائمة الكوبونات `[{id,code,discountPercent,isActive,expiresAt,maxUses,usedCount}]` |
| POST | `/api/coupons/validate` | — | `{code, subtotal}` | `{valid:true, code, discountPercent, discount}` أو `{valid:false, error}` |
| POST | `/api/coupons` | **Admin** | `{code, discountPercent, isActive?, expiresAt?, maxUses?}` upsert بالكود |
| DELETE | `/api/coupons/{id}` | **Admin** | حذف بالـ GUID |

## 11) الشحن والدفع
| Method | Path | Auth | الرد/Body |
|---|---|---|---|
| GET | `/api/shipping-zones` | — | المناطق النشطة `[{id,code,name,price,active}]` |
| GET | `/api/shipping-zones/all` | **Admin** | الكل (نشيط وغير نشيط) |
| POST | `/api/shipping-zones` | **Admin** | `{id?, code?, name, price, active}` upsert بالكود |
| DELETE | `/api/shipping-zones/{id}` | **Admin** | حذف |
| GET | `/api/payment-methods` | — | حاليًا COD واحد: `[{id:"cod", name:"الدفع عند الاستلام كاش", description:"..."}]` |
| GET | `/api/shipping-methods` | — | مشتق من المناطق النشطة `[{id,name,price,description}]` (فاضي لو مفيش مناطق) |

## 12) إعدادات المتجر (Ec_StoreSettingsList — صف واحد)
| Method | Path | Auth | الرد/Body |
|---|---|---|---|
| GET | `/api/admin/config` | — | `{storeName, storeTitle, promoTagline, logoUrl, primaryColor, freeShippingMin, announcementText, announcementEnabled, extraJson}` (قيم افتراضية عربية لو مفيش صف) |
| PUT | `/api/admin/config` | **Admin** | أي subset من نفس الحقول + `extraJson` (سلسلة JSON لأى إعدادات إضافية زي supportPhone/companyWhatsapp) — partial update |

## 13) إدارة المستخدمين (Admind)
| Method | Path | Body/Params |
|---|---|---|
| GET | `/api/admin/users?search=` | `[{uid, userId, email, name, fullName, phone, role("main_admin"/"admin"/"user"), points, isBlocked, authProvider, emailConfirmed, createdAt, lastLoginAt}]` |
| POST | `/api/admin/users/{userId}/block?blocked=true\|false` | حظر/فك حظر |
| POST | `/api/admin/users/{userId}/points` | `{points:int}` تعيين نقاط ولاء |

> **ملاحظة أدوار:** ما فيش endpoint لتغيير رول — الرول بتيتحدد من قائمة إيميلات الأدمن في إعدادات السيرفر (`Admins:MainAdminEmails` / `Admins:AdminEmails` أو env `ADMIN_MAIN_EMAILS`/`ADMIN_EMAILS`).

## 14) العربة المتروكة (Ec_AbandonedCarts)
| Method | Path | Auth | Body/Params |
|---|---|---|---|
| POST | `/api/abandoned-carts` | — (زائر) / Bearer | `{id?, userId?, customerName, customerPhone?, customerEmail?, governorate, total, items:[{name,price,quantity,image?}]}` — **idempotent**: نفس `userId`/`id` بيحدّث نفس الصف (لمسح السلة استخدم `DELETE /api/cart`). مع توكن: المفتاح إجباري هو `id` حساب التوكن والسيرفر بيتجاهل القيمة القادمة من الـ body؛ بدون توكن (زائر) المفتاح من الـ body زي الأول |
| GET | `/api/abandoned-carts` | **Admin** | آخر 500 عربة مع items |
| DELETE | `/api/abandoned-carts/{id}` | **Admin** | حذف بالـ GUID |

## 15) سجل النشاط (Ec_ActivityLogs)
| Method | Path | Auth | Body |
|---|---|---|---|
| GET | `/api/activity-logs?take=200` (max 1000) | **Admin** | `[{id, actorName, action, entityType, entityId, createdAt}]` |
| POST | `/api/activity-logs` | **Admin** | `{actorName?, action, entityType?, entityId?}` |
| DELETE | `/api/activity-logs` | **Admin** | مسح الكل `{success, removed}` |

## 16) التحليلات (Ec_AnalyticsSearches / Views)
| Method | Path | Body/Params | الرد |
|---|---|---|---|
| POST | `/api/analytics/search` | `{query}` | `{}` — بيعدّ مرات البحث عن نفس الكلمة |
| POST | `/api/analytics/view/{itemCode}` | — | `{}` — بعد مشاهدات الصنف |
| GET | `/api/analytics/report` | — | `{searches:{"term":count,...}, productViews:{"itemCode":count,...}}` |

## 17) البحث الذكي (AI — محلي بدون LLM)
| Method | Path | Body | الرد |
|---|---|---|---|
| POST | `/api/ai/smart-search` | `{query, products?}` — لو `products` متسرطيش بياخد الكتالوج كله من السيرفر | `{aiAdvice, recommendations:[{productId, matchReason, confidenceScore}]}` (max 4) |

## 18) إيميلات مساعدة (SMTP)
كلهم open للـ POST وبيستخدموا SMTP المحلّل لو مضبوط، وإلا بيرجعوا `{success:true}` كـ no-op:
`/api/email/welcome` `{email,name}` • `/api/email/verify` `{email,name,verificationLink}` • `/api/email/password-reset` `{email,name,resetLink}` • `/api/email/login-notification` `{email,...}` • `/api/email/order-status` `{order,newStatus,email,name}` • `/api/email/generic` `{toEmail,subject,title,messageHtml}` • `GET /api/email/test`

---

## نصائح للربط (اتطبقت في مشروع my ecommerce)
1. خزن `accessToken` وارميه في هيدر كل الطلبات؛ الـ refresh token ابعته لـ `/api/auth/refresh-token` قبل ما الـ access ينتهي (60 دقيقة) — مفيش endpoint بيعيد التوكن تلقائيًا.
2. السلة/المفضلة/العناوين: استخدم الـ `*/sync` عند أي تغيير من المتجر (batch واحد) بدل نداء لكل عنصر.
3. كوبون الشيك أوت: نادى `validate` قبل الإرسال واحسب `discount` منه، وابعتيه `couponCode` + `discountAmount` جوه الأوردر.
4. الأدمن UI: اطلب أي endpoint عليه Admin بنفس التوكن؛ لو 403 معناها الإيميل مش في قائمة الأدمن.
5. الأصناف المخفية أو جروبات بـ Status=0 مش هتظهر في أي نداء عام — مفيش داعي لفلترة إضافية.
