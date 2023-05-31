---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-03-16
--	Who: MarkusH
--	Details:
--	Composition fixes
---------------------------------------------------------------------

ALTER TABLE public.profile_composition
  ADD COLUMN opc_node_id character varying(256) NULL,
  ADD COLUMN symbolic_name character varying(256) COLLATE pg_catalog."default" NULL,
  ADD COLUMN metatags varchar NULL,
  ADD COLUMN document_url character varying(512) COLLATE pg_catalog."default" NULL;
