---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-09-27
--	Who: MarkusH
--	Details:
--	Data Type: is_json_scalar 
---------------------------------------------------------------------

ALTER TABLE public.data_type ADD COLUMN is_json_scalar boolean NULL;

ALTER TABLE public.profile ADD COLUMN character varying COLLATE pg_catalog."default" NULL;
ALTER TABLE public.profile_type_definition ADD COLUMN character varying(10) COLLATE pg_catalog."default" NULL;
ALTER TABLE public.profile_type_definition ADD COLUMN event_notifier integer NULL;

-- Just recreating the previous view unchanged: the dt.* then picks up the new data_type.is_json_scalar column.
DROP VIEW IF EXISTS public.v_data_type_rank;
CREATE VIEW public.v_data_type_rank
AS
select 
	--lu.name, 
	--ptd.*, 
	dt.*, 
    baseDt.id as base_data_type_id,
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
left outer join public.data_type baseDt on ptd.parent_id = baseDt.custom_type_id
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
    OWNER to profiledesigner;


