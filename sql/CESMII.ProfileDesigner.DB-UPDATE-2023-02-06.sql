---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-02-06
--	Who: MarkusH
--	Details:
--	Remove standard nodeset table
---------------------------------------------------------------------

ALTER TABLE public.profile
  ADD COLUMN cloud_library_id character varying(100)  NULL,
  ADD COLUMN cloud_library_pending_approval boolean NULL;

update public.profile as p
SET cloud_library_id = s.cloudlibrary_id
FROM public.standard_nodeset AS s
where p.ua_standard_profile_id = s.id

ALTER TABLE IF EXISTS public.profile DROP CONSTRAINT IF EXISTS profile_standard_profile_id;

ALTER TABLE public.profile
  DROP COLUMN ua_standard_profile_id;

DROP TABLE IF EXISTS public.standard_nodeset;

