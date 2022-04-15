---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2022-04-14
--	Who: Markus H
--	Details:
--	Remove owner_id column from tables that are not user-scoped
---------------------------------------------------------------------

ALTER TABLE public.lookup_type
DROP COLUMN owner_id

ALTER TABLE public.lookup
DROP COLUMN owner_id

ALTER TABLE public.lookup
DROP COLUMN owner_id

-- Remove unused opc_node_id column from profile_composition
ALTER TABLE public.profile_composition
DROP COLUMN opc_node_id
