---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2022-07-14
--	Who: Markus H
--	Details:
--	Add columns for import/export fidelity
---------------------------------------------------------------------

ALTER TABLE public.profile_type_definition
  ADD COLUMN is_option_set boolean NULL,
  ADD COLUMN variable_data_type_id integer NULL,
  ADD COLUMN variable_value_rank integer NULL,
  ADD COLUMN variable_array_dimensions character varying(256) NULL,
  ADD COLUMN variable_value character varying NULL,
  ADD CONSTRAINT prof_type_def_variable_data_type_fk_variable_data_type_id FOREIGN KEY (variable_data_type_id)
        REFERENCES public.profile_type_definition (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE public.profile_composition
  ADD COLUMN reference_is_inverse boolean NULL;

ALTER TABLE public.profile_attribute
  ADD COLUMN eng_unit_modeling_rule character varying(256) NULL,
  ADD COLUMN eu_range_nodeid character varying(256) NULL,
  ADD COLUMN eu_range_modeling_rule character varying(256) NULL,
  ADD COLUMN max_string_length integer NULL;
