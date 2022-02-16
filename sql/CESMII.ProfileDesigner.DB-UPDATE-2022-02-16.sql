---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2022-02-16
--	Who: Markus H
--	Details:
--	Make description fields unlimited length to accomodate unlimited length values coming in from nodesets
---------------------------------------------------------------------
DROP VIEW IF EXISTS public.v_data_type_rank;
DROP VIEW IF EXISTS public.v_engineering_unit_rank;

ALTER TABLE public.lookup_type
ALTER COLUMN description TYPE character varying; 

ALTER TABLE public.engineering_unit
ALTER COLUMN description TYPE character varying; 

ALTER TABLE public.profile_type_definition
ALTER COLUMN description TYPE character varying; 

ALTER TABLE public.profile_composition
ALTER COLUMN description TYPE character varying; 

ALTER TABLE public.data_type
ALTER COLUMN description TYPE character varying; 

ALTER TABLE public.profile_attribute
ALTER COLUMN description TYPE character varying; 

---------------------------------------------------------------------
--	View used by lookup data to allow us to get list of data types and rank
---------------------------------------------------------------------

--DROP VIEW IF EXISTS public.v_data_type_rank;

CREATE VIEW public.v_data_type_rank
AS
select 
	--lu.name, 
	--ptd.*, 
	dt.*, 
	COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) as popularity_index,
	--create a tiered system to distinguish between very popular and mildly popular and the others
	CASE 
		WHEN COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) > 40 THEN 3
		WHEN COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) > 20 THEN 2
		WHEN COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) > 10 THEN 1
		ELSE 0 END as popularity_level,
	COALESCE(a.usage_count, 0) as usage_count, 
	COALESCE(dtr.manual_rank, 0) as manual_rank
from public.data_type dt
left outer join public.data_type_rank dtr on dtr.data_type_id = dt.id
left outer join public.profile_type_definition ptd on ptd.id = dt.custom_type_id
--left outer join public.lookup lu on lu.id = ptd.type_id
left outer join (
	SELECT data_type_id, count(*) as usage_count 
	from public.profile_attribute 
	group by data_type_id
) a on a.data_type_id = dt.id
--order by 
-- 	 COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) desc
-- 	,COALESCE(a.usage_count, 0) desc 
--	,dt.display_order
--	,dt.name
;

ALTER VIEW public.v_data_type_rank
    OWNER to cesmii;

---------------------------------------------------------------------
--	View used by lookup data to allow us to get list of data types and rank
---------------------------------------------------------------------

--DROP VIEW IF EXISTS public.v_engineering_unit_rank;

CREATE VIEW public.v_engineering_unit_rank
AS
select 
	--lu.name, 
	--ptd.*, 
	eu.*, 
	COALESCE(a.usage_count, 0) + COALESCE(eur.manual_rank, 0) as popularity_index,
	--create a tiered system to distinguish between very popular and mildly popular and the others
	CASE 
		WHEN COALESCE(a.usage_count, 0) + COALESCE(eur.manual_rank, 0) > 40 THEN 3
		WHEN COALESCE(a.usage_count, 0) + COALESCE(eur.manual_rank, 0) > 20 THEN 2
		WHEN COALESCE(a.usage_count, 0) + COALESCE(eur.manual_rank, 0) > 10 THEN 1
		ELSE 0 END as popularity_level,
	COALESCE(a.usage_count, 0) as usage_count, 
	COALESCE(eur.manual_rank, 0) as manual_rank
from public.engineering_unit eu
left outer join public.engineering_unit_rank eur on eur.eng_unit_id = eu.id
left outer join (
	SELECT eng_unit_id, count(*) as usage_count 
	from public.profile_attribute 
	group by eng_unit_id
) a on a.eng_unit_id = eu.id
;

ALTER VIEW public.v_engineering_unit_rank
    OWNER to cesmii;
