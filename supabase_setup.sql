-- =============================================================
--  BIMLab Studio – Supabase Setup Script
--  Dán toàn bộ file này vào Supabase SQL Editor và bấm Run.
-- =============================================================

-- 1. Tạo bảng authorized_leaders
CREATE TABLE IF NOT EXISTS public.authorized_leaders (
    email       TEXT        PRIMARY KEY,
    full_name   TEXT        NOT NULL DEFAULT '',
    is_active   BOOLEAN     NOT NULL DEFAULT true,
    can_manage  BOOLEAN     NOT NULL DEFAULT false,
    added_by    TEXT        REFERENCES public.authorized_leaders(email) ON DELETE SET NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login  TIMESTAMPTZ
);

-- 2. Bật Row Level Security
ALTER TABLE public.authorized_leaders ENABLE ROW LEVEL SECURITY;

-- 3. Policy: anon key chỉ được SELECT (dùng khi login check)
--    INSERT / UPDATE / DELETE đi qua service_role key (bypass RLS)
DROP POLICY IF EXISTS "anon_read_only" ON public.authorized_leaders;
CREATE POLICY "anon_read_only"
    ON public.authorized_leaders
    FOR SELECT
    USING (true);

-- 4. Index để query theo email nhanh hơn (email đã là PK nên index tự có,
--    thêm index cho is_active để filter nhanh hơn khi login check)
CREATE INDEX IF NOT EXISTS idx_leaders_active
    ON public.authorized_leaders (email, is_active);

-- =============================================================
--  SEED DATA — Thêm tài khoản đầu tiên (super admin)
--  ► Sửa email và full_name bên dưới trước khi chạy!
-- =============================================================
INSERT INTO public.authorized_leaders (email, full_name, is_active, can_manage, added_by)
VALUES ('your_email@company.com', 'Tên của bạn', true, true, NULL)
ON CONFLICT (email) DO NOTHING;

-- =============================================================
--  KIỂM TRA — Chạy sau khi setup xong để xác nhận
-- =============================================================
-- SELECT * FROM public.authorized_leaders;
