---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-03-16
--	Who: MarkusH
--	Details:
--	Composition fixes
---------------------------------------------------------------------

ALTER TABLE public.profile
  ADD COLUMN xml_schema_uri character varying(400) COLLATE pg_catalog."default" NULL
  ;
